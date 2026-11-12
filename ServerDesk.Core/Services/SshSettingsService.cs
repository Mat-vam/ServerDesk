using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using ServerDesk.Core.Errors;

namespace ServerDesk.Core.Services;

public sealed class SshSettingsService
{
    private readonly DeviceService _deviceService;
    private readonly ICredentialStore _credentialStore;

    public SshSettingsService(
        DeviceService deviceService,
        ICredentialStore credentialStore)
    {
        _deviceService = deviceService;
        _credentialStore = credentialStore;
    }

    public string? LoadSavedPassphrase(Device device)
    {
        if (device.SshSettings?.RememberPassphrase != true)
            return null;

        return _credentialStore.Load(device.Id);
    }

    public async Task SaveAsync(
        Device device,
        string keyPath,
        string fingerprint,
        string passphrase,
        bool rememberPassphrase,
        CancellationToken cancellationToken = default)
    {
        var settings = new SshConnectionSettings
        {
            PrivateKeyPath = keyPath.Trim(),
            HostFingerprint = fingerprint.Trim(),
            RememberPassphrase = rememberPassphrase,
            AutoConnect = device.SshSettings?.AutoConnect ?? false
        };

        if (rememberPassphrase)
            _credentialStore.Save(device.Id, passphrase);
        else
            _credentialStore.Delete(device.Id);

        await PersistSettingsAsync(
            device,
            settings,
            cancellationToken);
    }

    public async Task ForgetPassphraseAsync(
        Device device,
        CancellationToken cancellationToken = default)
    {
        var previous = device.SshSettings
            ?? new SshConnectionSettings();

        var settings = new SshConnectionSettings
        {
            PrivateKeyPath = previous.PrivateKeyPath,
            HostFingerprint = previous.HostFingerprint,
            RememberPassphrase = false,
            AutoConnect = previous.AutoConnect
        };

        _credentialStore.Delete(device.Id);

        await PersistSettingsAsync(
            device,
            settings,
            cancellationToken);
    }

    private async Task PersistSettingsAsync(
        Device device,
        SshConnectionSettings settings,
        CancellationToken cancellationToken)
    {
        bool saved = await _deviceService.UpdateSshSettingsAsync(
            device.Id,
            settings,
            cancellationToken);

        if (!saved)
            throw new AppException(AppErrorCode.DeviceNotFound);

        device.SshSettings = settings;
    }
}