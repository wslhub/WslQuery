using System;
using System.Runtime.InteropServices;

namespace WslQuery
{
    internal static class NativeMethods
    {
        [DllImport("wslapi.dll",
            CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WslIsDistributionRegistered(
            string distributionName);

        [DllImport("wslapi.dll",
            CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            PreserveSig = true)]
        public static extern int WslGetDistributionConfiguration(
            string distributionName,
            [MarshalAs(UnmanagedType.I4)] out int distributionVersion,
            [MarshalAs(UnmanagedType.I4)] out int defaultUID,
            [MarshalAs(UnmanagedType.I4)] out WslDistributionFlags wslDistributionFlags,
            out IntPtr defaultEnvironmentVariables,
            [MarshalAs(UnmanagedType.I4)] out int defaultEnvironmentVariableCount);
    }
}
