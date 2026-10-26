namespace ServerDesk.Core.Models;

public sealed class Device
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 22;

    public string Username { get; set; } = string.Empty;

    public SshConnectionSettings SshSettings { get; set; } = new();

}
