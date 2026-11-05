using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using ServerDesk.Core.Errors;

namespace ServerDesk.Core.Services;

public sealed class DeviceService
{
    private readonly IDeviceRepository _repository;

    public DeviceService(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Device>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public async Task<Device> AddDeviceAsync(string name, string host, int port, string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException(AppErrorCode.DeviceNameRequired);
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new AppException(AppErrorCode.DeviceHostRequired);
        }

        if (port < 1 || port > 65535)
        {
            throw new AppException(AppErrorCode.DevicePortOutOfRange);
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new AppException(AppErrorCode.DeviceUsernameRequired);
        }

        var devices = (await _repository.GetAllAsync(cancellationToken)).ToList();

        var device = new Device
        {
            Name = name.Trim(),
            Host = host.Trim(),
            Port = port,
            Username = username.Trim()
        };

        devices.Add(device);

        await _repository.SaveAsync(devices, cancellationToken);

        return device;
    }

    public async Task<bool> RemoveDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var devices = (await _repository.GetAllAsync(cancellationToken)).ToList();

        int removed = devices.RemoveAll(d => d.Id == id);

        if (removed == 0)
        {
            return false;
        }

        await _repository.SaveAsync(devices, cancellationToken);

        return true;
    }

    public async Task<bool> UpdateDeviceAsync(Guid id, string name, string host, int port, string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException(AppErrorCode.DeviceNameRequired);
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new AppException(AppErrorCode.DeviceHostRequired);
        }

        if (port < 1 || port > 65535)
        {
            throw new AppException(AppErrorCode.DevicePortOutOfRange);
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new AppException(AppErrorCode.DeviceUsernameRequired);
        }

        var devices = (await _repository.GetAllAsync(
            cancellationToken)).ToList();

        var device = devices.FirstOrDefault(d => d.Id == id);

        if (device is null)
            return false;

        device.Name = name.Trim();
        device.Host = host.Trim();
        device.Port = port;
        device.Username = username.Trim();

        await _repository.SaveAsync(devices, cancellationToken);

        return true;
    }


    public async Task<bool> UpdateSshSettingsAsync( Guid deviceId, SshConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var devices = (await _repository.GetAllAsync(
            cancellationToken)).ToList();

        var device = devices.FirstOrDefault(
            d => d.Id == deviceId);

        if (device is null)
            return false;

        device.SshSettings = new SshConnectionSettings
        {
            PrivateKeyPath = settings.PrivateKeyPath.Trim(),
            HostFingerprint = settings.HostFingerprint.Trim(),
            RememberPassphrase = settings.RememberPassphrase,
            AutoConnect = settings.AutoConnect
        };

        await _repository.SaveAsync(
            devices,
            cancellationToken);

        return true;
    }

    public async Task<Guid> SaveDeviceAsync(
        Guid? deviceId,
        string name,
        string host,
        int port,
        string username,
        CancellationToken cancellationToken = default)
    {
        if (deviceId is Guid existingId)
        {
            bool updated = await UpdateDeviceAsync(
                existingId,
                name,
                host,
                port,
                username,
                cancellationToken);

            if (!updated)
            {
                throw new AppException(AppErrorCode.DeviceNotFound);
            }

            return existingId;
        }

        Device device = await AddDeviceAsync(
            name,
            host,
            port,
            username,
            cancellationToken);

        return device.Id;
    }
}
