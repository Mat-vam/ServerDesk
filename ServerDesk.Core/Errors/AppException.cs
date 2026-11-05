namespace ServerDesk.Core.Errors;

public sealed class AppException : Exception
{
    public AppErrorCode Code { get; }
    public string? Details { get; }

    public AppException(
        AppErrorCode code,
        string? details = null,
        Exception? innerException = null)
        : base(code.ToString(), innerException)
    {
        Code = code;
        Details = details;
    }
}