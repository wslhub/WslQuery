using System;
using System.Collections.Generic;

namespace WslQuery
{
    public sealed class DistroInfo
    {
        public bool Succeed => HResult == 0;
        public string BasePath { get; set; }
        public string DistroName { get; set; }
        public List<string> DefaultEnvironmentVariables { get; set; } = new List<string>();
        public List<string> KernelCommandLine { get; set; } = new List<string>();
        public int DefaultUid { get; set; }
        public bool EnableInterop => DistroFlags.HasFlag(WslDistributionFlags.EnableInterop);
        public bool EnableDriveMounting => DistroFlags.HasFlag(WslDistributionFlags.EnableDriveMouting);
        public bool AppendNtPath => DistroFlags.HasFlag(WslDistributionFlags.AppendNtPath);
        public WslDistributionFlags DistroFlags { get; set; }
        public Guid DistroId { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsDefaultDistro { get; set; }
        public int HResult { get; set; }
        public int WslVersion { get; set; }
    }
}
