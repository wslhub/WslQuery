using System.Runtime.InteropServices;

namespace WslQuery;

internal static class ComSecurity
{
    internal const int AlreadyInitialized = unchecked((int)0x80010119); // RPC_E_TOO_LATE

    internal static void ThrowIfInitializationFailed(int result)
    {
        // COM security is process-wide and cannot be replaced after initialization.
        // Continue with the existing policy; any subsequent WSL failure is still reported.
        if (result != AlreadyInitialized)
            Marshal.ThrowExceptionForHR(result);
    }
}
