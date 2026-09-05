using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WslQuery;

[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    [LibraryImport("ole32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport("ole32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial void CoUninitialize();

    [LibraryImport("ole32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int CoInitializeSecurity(nint securityDescriptor, int authServiceCount,
        nint authServices, nint reserved1, uint authenticationLevel, uint impersonationLevel,
        nint authList, uint capabilities, nint reserved3);

    [LibraryImport("wslapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WslIsDistributionRegistered(string distributionName);

    [LibraryImport("wslapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int WslGetDistributionConfiguration(string distributionName,
        out uint distributionVersion, out uint defaultUid, out uint flags,
        out nint environmentVariables, out uint environmentVariableCount);
}
