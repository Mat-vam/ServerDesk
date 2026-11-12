using System.Text.Json;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;

namespace ServerDesk.Infrastructure.Storage;

public sealed class JsonAppSettingsRepository : IAppSettingsRepository
{
    private readonly string _filePath;

    public JsonAppSettingsRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<AppSettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            await using var stream = File.OpenRead(_filePath);

            return await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                cancellationToken: cancellationToken)
                ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Użycie ustawień domyślnych po błędzie odczytu JSON.
            return new AppSettings();
        }
    }

    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporaryPath =
            _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    },
                    cancellationToken);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}