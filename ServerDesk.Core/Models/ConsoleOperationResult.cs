namespace ServerDesk.Core.Models;

public sealed record ConsoleOperationResult(
    string? Output,
    bool CommandSent,
    Exception? RefreshError = null);