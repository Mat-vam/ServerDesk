using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Errors;

namespace ServerDesk.Infrastructure.Security;
public sealed class WindowsCredentialStore : ICredentialStore
{
    private const uint GenericCredential = 1;
    private const uint PersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    private static string Target(Guid deviceId)
        => $"ServerDesk:SSH:Passphrase:{deviceId:N}";

    public void Save(Guid deviceId, string passphrase)
    {
        EnsureWindows();

        byte[] bytes = Encoding.Unicode.GetBytes(passphrase);

        if (bytes.Length > 2560)
            throw new AppException(AppErrorCode.PassphraseTooLong);

        IntPtr blob = IntPtr.Zero;

        try
        {
            if (bytes.Length > 0)
            {
                blob = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, blob, bytes.Length);
            }

            var credential = new NativeCredential
            {
                Type = GenericCredential,
                TargetName = Target(deviceId),
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blob,
                Persist = PersistLocalMachine,
                UserName = Environment.UserName
            };

            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
        }
        finally
        {
            if (blob != IntPtr.Zero)
            {
                for (int i = 0; i < bytes.Length; i++)
                    Marshal.WriteByte(blob, i, 0);

                Marshal.FreeHGlobal(blob);
            }

            Array.Clear(bytes);
        }
    }

    public string? Load(Guid deviceId)
    {
        EnsureWindows();

        if (!CredRead(
            Target(deviceId),
            GenericCredential,
            0,
            out IntPtr pointer))
        {
            int error = Marshal.GetLastWin32Error();

            if (error == ErrorNotFound)
                return null;

            throw new Win32Exception(error);
        }

        try
        {
            var credential =
                Marshal.PtrToStructure<NativeCredential>(pointer);

            if (credential.CredentialBlobSize == 0)
                return string.Empty;

            if (credential.CredentialBlobSize > 2560 || credential.CredentialBlobSize % 2 != 0 || credential.CredentialBlob == IntPtr.Zero)
                throw new AppException(AppErrorCode.InvalidStoredCredential);

            int characters =
                checked((int)credential.CredentialBlobSize / 2);

            return Marshal.PtrToStringUni(
                credential.CredentialBlob,
                characters);
        }
        finally
        {
            CredFree(pointer);
        }
    }

    public void Delete(Guid deviceId)
    {
        EnsureWindows();

        if (CredDelete(
            Target(deviceId),
            GenericCredential,
            0))
        {
            return;
        }

        int error = Marshal.GetLastWin32Error();

        if (error != ErrorNotFound)
            throw new Win32Exception(error);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new AppException(AppErrorCode.CredentialStoreWindowsOnly);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;

        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? UserName;
    }

    [DllImport(
        "Advapi32.dll",
        EntryPoint = "CredWriteW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(
        ref NativeCredential credential,
        uint flags);

    [DllImport(
        "Advapi32.dll",
        EntryPoint = "CredReadW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(
        string target,
        uint type,
        uint flags,
        out IntPtr credential);

    [DllImport(
        "Advapi32.dll",
        EntryPoint = "CredDeleteW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(
        string target,
        uint type,
        uint flags);

    [DllImport("Advapi32.dll")]
    private static extern void CredFree(IntPtr credential);
}
