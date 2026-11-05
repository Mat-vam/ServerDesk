namespace ServerDesk.Core.Interfaces;

public interface ICredentialStore
{
    void Save(Guid deviceId, string passphrase);
    string? Load(Guid deviceId);
    void Delete(Guid deviceId);
}