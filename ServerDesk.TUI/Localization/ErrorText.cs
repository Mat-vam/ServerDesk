using ServerDesk.Core.Errors;

namespace ServerDesk.TUI.Localization;

public static class ErrorText
{
    public static string Get(Exception exception)
    {
        if (exception is not AppException appException)
            return exception.Message;

        string resourceKey = appException.Code switch
        {
            AppErrorCode.DeviceNameRequired =>
                "DeviceNameRequired",

            AppErrorCode.DeviceHostRequired =>
                "DeviceHostRequired",

            AppErrorCode.DeviceUsernameRequired =>
                "DeviceUsernameRequired",

            AppErrorCode.DevicePortOutOfRange =>
                "DevicePortOutOfRange",

            AppErrorCode.DeviceNotFound =>
                "DeviceNotFound",

            AppErrorCode.InvalidCommand =>
                "InvalidCommand",

            AppErrorCode.PrivateKeyPathRequired =>
                "PrivateKeyPathRequired",

            AppErrorCode.HostFingerprintRequired =>
                "HostFingerprintRequired",

            AppErrorCode.ConnectionSettingsChanged =>
                "ConnectionSettingsChanged",

            AppErrorCode.InvalidSessionName =>
                "InvalidSessionName",

            AppErrorCode.InvalidSessionId =>
                "InvalidSessionId",

            AppErrorCode.InvalidHostFingerprint =>
                "InvalidHostFingerprint",

            AppErrorCode.SshSessionListFailed =>
                "SshSessionListFailed",

            AppErrorCode.SshSessionCreationFailed =>
                "SshSessionCreationFailed",

            AppErrorCode.SshCommandSendFailed =>
                "SshCommandSendFailed",

            AppErrorCode.SshConsoleReadFailed =>
                "SshConsoleReadFailed",

            AppErrorCode.SshSessionTerminationFailed =>
                "SshSessionTerminationFailed",

            AppErrorCode.PassphraseTooLong =>
                "PassphraseTooLong",

            AppErrorCode.InvalidStoredCredential =>
                "InvalidStoredCredential",

            AppErrorCode.CredentialStoreWindowsOnly =>
                "CredentialStoreWindowsOnly",

            AppErrorCode.SshHardcopyMissing =>
                "SshHardcopyMissing",
                
            AppErrorCode.InvalidLanguage =>
                "InvalidLanguage",

            _ => throw new InvalidOperationException(
                $"Missing error translation mapping: {appException.Code}")
        };

        string message = UiText.Get(resourceKey);

        return string.IsNullOrWhiteSpace(appException.Details)
            ? message
            : UiText.Format(
                "ErrorWithDetails",
                message,
                appException.Details);
    }
}