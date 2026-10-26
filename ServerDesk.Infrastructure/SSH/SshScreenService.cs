using System.Text.RegularExpressions;
using Renci.SshNet;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;

namespace ServerDesk.Infrastructure.SSH;

public sealed class SshScreenService : IScreenService
{
    public async Task<IReadOnlyList<ScreenSession>> GetSessionsAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new ArgumentException("Brak zaufanego odcisku klucza serwera.");

        using var privateKey = new PrivateKeyFile(
            privateKeyPath,
            passphrase
        );

        using var client = new SshClient(
            device.Host,
            device.Port,
            device.Username,
            privateKey
        );

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);

        client.HostKeyReceived += (_, e) =>
        {
            e.CanTrust = string.Equals(
                e.FingerPrintSHA256,
                expectedHostFingerprint.Replace("SHA256:", "").Trim(),
                StringComparison.Ordinal
            );
        };

        await client.ConnectAsync(cancellationToken);

        using var command = client.CreateCommand("screen -ls");
        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        // screen -ls moze zwrocic kod 1, kiedy nie ma sesji.
        if (command.ExitStatus != 0 &&
            command.ExitStatus != 1)
        {
            throw new InvalidOperationException(
                $"Nie mozna pobrac sesji: {command.Error}");
        }

        string output = command.Result + "\n" + command.Error;

        return ParseSessions(output);
    }


    public static IReadOnlyList<ScreenSession> ParseSessions(
        string output)
    {
        var result = new List<ScreenSession>();

        foreach (string line in output.Split('\n'))
        {
            var match = Regex.Match(
                line.TrimEnd('\r'),
                @"^\s*(?<id>\d+\.(?<name>\S+))\s+.*\((?<state>Detached|Attached)\)\s*$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                continue;

            result.Add(new ScreenSession
            {
                Id = match.Groups["id"].Value,
                Name = match.Groups["name"].Value,
                State = match.Groups["state"].Value
            });
        }

        return result;
    }


    public async Task CreateTestSessionAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionName, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(
            sessionName,
            @"^[a-zA-Z0-9_-]{1,32}$"))
        {
            throw new ArgumentException(
                "Nazwa sesji moze zawierac tylko litery, " +
                "cyfry, _ i - (maksymalnie 32 znaki).");
        }

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new ArgumentException(
                "Brak zaufanego odcisku klucza hosta.");

        using var privateKey = new PrivateKeyFile(
            privateKeyPath,
            passphrase);

        using var client = new SshClient(
            device.Host,
            device.Port,
            device.Username,
            privateKey);

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);

        client.HostKeyReceived += (_, e) =>
        {
            e.CanTrust = string.Equals(
                e.FingerPrintSHA256,
                expectedHostFingerprint
                    .Replace("SHA256:", "")
                    .Trim(),
                StringComparison.Ordinal
            );
        };

        await client.ConnectAsync(cancellationToken);

        // Uruchamiamy bezpieczna sesje testowa,
        // ktora samoczynnie zakonczy sie po 600 sekundach.
        using var command = client.CreateCommand(
            $"screen -dmS {sessionName} sleep 600");

        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
        {
            throw new InvalidOperationException(
                $"Nie mozna utworzyc sesji: {command.Error}");
        }
    }


    public async Task SendCommandAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, string text, CancellationToken cancellationToken = default)
    {
        // Identyfikator sesji musi miec oczekiwany format.
        if (!Regex.IsMatch(
            sessionId,
            @"^\d+\.[A-Za-z0-9_.-]+$"))
        {
            throw new ArgumentException(
                "Nieprawidlowy identyfikator sesji.");
        }

        if (string.IsNullOrWhiteSpace(text) ||
            text.Length > 512 ||
            text.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Polecenie musi miec 1-512 znakow " +
                "i nie moze zawierac znakow sterujacych.");
        }

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new ArgumentException(
                "Brak odcisku klucza hosta.");

        using var privateKey = new PrivateKeyFile(
            privateKeyPath, passphrase);

        using var client = new SshClient(
            device.Host,
            device.Port,
            device.Username,
            privateKey);

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);

        client.HostKeyReceived += (_, e) =>
        {
            e.CanTrust = string.Equals(
                e.FingerPrintSHA256,
                expectedHostFingerprint
                    .Replace("SHA256:", "")
                    .Trim(),
                StringComparison.Ordinal);
        };

        await client.ConnectAsync(cancellationToken);

        // Bezpieczne cytowanie argumentow powloki.
        static string Quote(string value)
            => "'" + value.Replace("'", "'\"'\"'") + "'";

        string id = Quote(sessionId);
        string commandText = Quote(text);

        // Wyslanie tekstu, a nastepnie klawisza Enter.
        string remoteCommand =
            $"screen -S {id} -p 0 -X stuff {commandText}" +
            $" && screen -S {id} -p 0 -X stuff \"$(printf '\\r')\"";

        using var command = client.CreateCommand(remoteCommand);
        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
        {
            throw new InvalidOperationException(
                "Nie udalo sie wyslac polecenia: " +
                command.Error);
        }
    }


}
