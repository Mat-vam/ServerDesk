using System.Text.RegularExpressions;
using Renci.SshNet;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using ServerDesk.Core.Errors;

namespace ServerDesk.Infrastructure.SSH;

public sealed class SshScreenService : IScreenService
{
    public async Task<IReadOnlyList<ScreenSession>> GetSessionsAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new AppException(AppErrorCode.HostFingerprintRequired);

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

        // Kod 1 może oznaczać brak sesji.
        if (command.ExitStatus != 0 && command.ExitStatus != 1)
            throw new AppException(AppErrorCode.SshSessionListFailed, command.Error.Trim());

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


    public async Task CreateSessionAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionName, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(sessionName, @"^[a-zA-Z0-9_-]{1,32}$"))
            throw new AppException(AppErrorCode.InvalidSessionName);

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new AppException(AppErrorCode.HostFingerprintRequired);

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

        static string Quote(string value)
            => "'" + value.Replace("'", "'\"'\"'") + "'";

        // Sesja działa w tle po zamknięciu ServerDesk.
        using var command = client.CreateCommand(
            $"screen -dmS {Quote(sessionName)} /bin/bash -i");

        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
            throw new AppException(AppErrorCode.SshSessionCreationFailed, command.Error.Trim());
    }


    public async Task SendCommandAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, string text, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(sessionId, @"^\d+\.[A-Za-z0-9_.-]+$"))
            throw new AppException(AppErrorCode.InvalidSessionId);

        if (string.IsNullOrWhiteSpace(text) || text.Length > 512 || text.Any(char.IsControl))
            throw new AppException(AppErrorCode.InvalidCommand);

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new AppException(AppErrorCode.HostFingerprintRequired);

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

        static string Quote(string value)
            => "'" + value.Replace("'", "'\"'\"'") + "'";

        string id = Quote(sessionId);
        string commandText = Quote(text);

        // Wysłanie tekstu i zatwierdzenie klawiszem Enter.
        string remoteCommand =
            $"screen -S {id} -p 0 -X stuff {commandText}" +
            $" && screen -S {id} -p 0 -X stuff \"$(printf '\\r')\"";

        using var command = client.CreateCommand(remoteCommand);
        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
            throw new AppException(AppErrorCode.SshCommandSendFailed, command.Error.Trim());
    }

    public async Task<string> GetConsoleOutputAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !Regex.IsMatch(sessionId, @"^\d+\.[A-Za-z0-9_.-]+$"))
            throw new AppException(AppErrorCode.InvalidSessionId);

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new AppException(AppErrorCode.HostFingerprintRequired);

        string fingerprint = expectedHostFingerprint.Trim();

        if (fingerprint.StartsWith("SHA256:", StringComparison.Ordinal))
            fingerprint = fingerprint["SHA256:".Length..];

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new AppException(AppErrorCode.InvalidHostFingerprint);
        }

        static string Quote(string value)
            => "'" + value.Replace("'", "'\"'\"'") + "'";

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
                fingerprint,
                StringComparison.Ordinal);
        };

        await client.ConnectAsync(cancellationToken);

        string id = Quote(sessionId);

        // Losowy katalog izoluje równoległe odczyty. Pułapka usuwa pliki po zakończeniu skryptu.
        string script = $$"""
        set -eu
        umask 077

        tmpdir=$(mktemp -d /tmp/serverdesk-console.XXXXXXXXXX)

        cleanup() {
            rm -f -- "$tmpdir/output.txt"
            rmdir -- "$tmpdir"
        }

        trap cleanup EXIT
        trap 'exit 129' HUP
        trap 'exit 130' INT
        trap 'exit 143' TERM

        screen -S {{id}} -p 0 -X hardcopy "$tmpdir/output.txt"

        # Zapytanie do tej samej sesji pozwala zaczekać na zapis pliku.
        screen -S {{id}} -p 0 -Q info >/dev/null

        if [ ! -f "$tmpdir/output.txt" ]; then
            printf '%s\n' 'SERVERDESK_HARDCOPY_MISSING' >&2
            exit 1
        fi

        cat -- "$tmpdir/output.txt"
        """;

        script = script.Replace("\r\n", "\n").Replace("\r", "\n");

        using var command = client.CreateCommand(
            "sh -c " + Quote(script));

        command.CommandTimeout = TimeSpan.FromSeconds(15);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
        {
            string details = command.Error.Trim();

            bool hardcopyMissing = details
                .Split('\n')
                .Any(line => string.Equals(
                    line.Trim(),
                    "SERVERDESK_HARDCOPY_MISSING",
                    StringComparison.Ordinal));

            if (hardcopyMissing)
                throw new AppException(AppErrorCode.SshHardcopyMissing);

            throw new AppException(
                AppErrorCode.SshConsoleReadFailed,
                details);
        }

        return command.Result;
    }

    public async Task TerminateSessionAsync(
        Device device,
        string privateKeyPath,
        string passphrase,
        string expectedHostFingerprint,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !Regex.IsMatch(sessionId, @"^\d+\.[A-Za-z0-9_.-]+$"))
            throw new AppException(AppErrorCode.InvalidSessionId);

        if (string.IsNullOrWhiteSpace(expectedHostFingerprint))
            throw new AppException(AppErrorCode.HostFingerprintRequired);

        string fingerprint = expectedHostFingerprint.Trim();

        if (fingerprint.StartsWith("SHA256:", StringComparison.Ordinal))
            fingerprint = fingerprint["SHA256:".Length..];

        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new AppException(AppErrorCode.InvalidHostFingerprint);

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
                fingerprint,
                StringComparison.Ordinal);
        };

        await client.ConnectAsync(cancellationToken);

        static string Quote(string value)
            => "'" + value.Replace("'", "'\"'\"'") + "'";

        using var command = client.CreateCommand(
            $"screen -S {Quote(sessionId)} -X quit");

        command.CommandTimeout = TimeSpan.FromSeconds(10);

        await command.ExecuteAsync(cancellationToken);

        if (command.ExitStatus != 0)
        {
            string details = (command.Error + "\n" + command.Result).Trim();

            throw new AppException(AppErrorCode.SshSessionTerminationFailed, details);
        }
    }

}
