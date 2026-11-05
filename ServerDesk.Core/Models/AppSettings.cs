namespace ServerDesk.Core.Models;

public sealed record AppSettings
{
    public string Language { get; init; } = "pl";
}