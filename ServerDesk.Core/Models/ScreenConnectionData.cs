namespace ServerDesk.Core.Models;

public sealed record ScreenConnectionData(
    string KeyPath,
    string Passphrase,
    string Fingerprint,
    bool RememberPassphrase)
{
    // Reprezentacja tekstowa pomija hasło klucza.
    public override string ToString() => nameof(ScreenConnectionData);
}