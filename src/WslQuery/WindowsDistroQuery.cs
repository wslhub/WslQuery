using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WslQuery;

[SupportedOSPlatform("windows")]
internal static class WindowsDistroQuery
{
    private const string LxssPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";
    private const int DistributionNotFound = unchecked((int)0x80070002);

    internal static List<DistroInfo> Query()
    {
        // Resolve the DLL before querying the registry so missing WSL is an error, not an empty list.
        var library = NativeLibrary.Load("wslapi.dll", typeof(NativeMethods).Assembly, DllImportSearchPath.System32);
        NativeLibrary.Free(library);

        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var lxss = currentUser.OpenSubKey(LxssPath, writable: false);
        var results = ReadRegistry(lxss);
        if (results.Count == 0)
            return results;

        Marshal.ThrowExceptionForHR(NativeMethods.CoInitializeEx(0, 0)); // COINIT_MULTITHREADED
        try
        {
            // Preserve the WSL client impersonation and static-cloaking settings.
            Marshal.ThrowExceptionForHR(NativeMethods.CoInitializeSecurity(0, -1, 0, 0, 0, 3, 0, 0x20, 0));
            foreach (var distro in results)
                PopulateConfiguration(distro);
        }
        finally
        {
            NativeMethods.CoUninitialize();
        }
        return results;
    }

    internal static List<DistroInfo> ReadRegistry(RegistryKey? lxss)
    {
        var results = new List<DistroInfo>();
        if (lxss is null)
            return results;

        Guid.TryParse(lxss.GetValue("DefaultDistribution") as string, out var defaultId);
        foreach (var keyName in lxss.GetSubKeyNames())
        {
            if (!Guid.TryParse(keyName, out var distroId))
                continue;

            using var key = lxss.OpenSubKey(keyName, writable: false);
            if (key?.GetValue("DistributionName") is not string name || string.IsNullOrWhiteSpace(name))
                continue;

            results.Add(new DistroInfo
            {
                DistroId = distroId,
                DistroName = name,
                BasePath = key.GetValue("BasePath") as string,
                KernelCommandLine = (key.GetValue("KernelCommandLine") as string ?? "")
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).ToList(),
                IsDefault = distroId == defaultId,
                HResult = DistributionNotFound
            });
        }
        return results;
    }

    private static void PopulateConfiguration(DistroInfo distro)
    {
        distro.IsRegistered = NativeMethods.WslIsDistributionRegistered(distro.DistroName);
        if (!distro.IsRegistered)
            return;

        distro.HResult = NativeMethods.WslGetDistributionConfiguration(distro.DistroName,
            out var version, out var uid, out var flags, out var variables, out var count);
        // The API only guarantees ownership of its output buffers after a successful call.
        if (distro.HResult != 0)
            return;

        distro.DefaultEnvironmentVariables = NativeEnvironment.ReadAndFree(variables, count);
        distro.WslVersion = version;
        distro.DefaultUid = uid;
        distro.DistroFlags = flags;
    }
}

internal static class NativeEnvironment
{
    internal static List<string> ReadAndFree(nint variables, uint count)
    {
        if (variables == 0)
        {
            if (count != 0)
                throw new InvalidDataException("WSL returned an invalid environment variable array.");
            return [];
        }

        try
        {
            var results = new List<string>();
            for (uint index = 0; index < count; index++)
            {
                var pointer = Marshal.ReadIntPtr(variables, checked((int)index * IntPtr.Size));
                results.Add(Marshal.PtrToStringUTF8(pointer) ?? "");
            }
            return results;
        }
        finally
        {
            for (uint index = 0; index < count; index++)
                Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(variables, checked((int)index * IntPtr.Size)));
            Marshal.FreeCoTaskMem(variables);
        }
    }
}
