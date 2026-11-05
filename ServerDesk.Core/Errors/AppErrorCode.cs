namespace ServerDesk.Core.Errors;

public enum AppErrorCode
{
    DeviceNameRequired,
    DeviceHostRequired,
    DeviceUsernameRequired,
    DevicePortOutOfRange,
    DeviceNotFound,
    InvalidCommand,
    PrivateKeyPathRequired,
    HostFingerprintRequired,
    ConnectionSettingsChanged,
    InvalidSessionName,
    InvalidSessionId,
    InvalidHostFingerprint,
    SshSessionListFailed,
    SshSessionCreationFailed,
    SshCommandSendFailed,
    SshConsoleReadFailed,
    SshSessionTerminationFailed,
    PassphraseTooLong,
    InvalidStoredCredential,
    CredentialStoreWindowsOnly,
    SshHardcopyMissing,
    InvalidLanguage
}