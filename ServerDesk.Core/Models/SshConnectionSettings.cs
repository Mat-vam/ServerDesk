namespace ServerDesk.Core.Models;
public sealed class SshConnectionSettings
{
    public string PrivateKeyPath { get; set; } = string.Empty;

    public string HostFingerprint { get; set; } = string.Empty;

    public bool RememberPassphrase { get; set; } = false;

    public bool AutoConnect { get; set; } = false;
}
