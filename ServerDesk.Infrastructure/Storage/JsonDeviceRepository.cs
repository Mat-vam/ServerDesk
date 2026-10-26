using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using System.Text.Json;
namespace ServerDesk.Infrastructure.Storage;

public sealed class JsonDeviceRepository : IDeviceRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonDeviceRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<Device>();
            }

            await using var stream = File.OpenRead(_filePath);

            var devices = await JsonSerializer.DeserializeAsync<List<Device>>(stream, cancellationToken: cancellationToken);

            return devices ?? new List<Device>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(IReadOnlyList<Device> devices, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                await using (var stream = File.Create(tempPath))
                {
                    await JsonSerializer.SerializeAsync(stream, devices, options, cancellationToken);
                }

                File.Move(tempPath, _filePath, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
