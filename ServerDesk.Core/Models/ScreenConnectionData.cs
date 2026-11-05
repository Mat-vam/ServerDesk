namespace ServerDesk.Core.Models;

public sealed record ScreenConnectionData(
    string KeyPath,
    string Passphrase,
    string Fingerprint,
    bool RememberPassphrase)
{
    // Automatyczny ToString rekordu nie powinien ujawniac hasla.
    public override string ToString() => nameof(ScreenConnectionData);
}