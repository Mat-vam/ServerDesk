namespace ServerDesk.Core.Models;

public sealed record ScreenSessionsResult(
    IReadOnlyList<ScreenSession>? Sessions,
    Exception? RefreshError = null,
    bool SessionStillListed = false);