using System;

namespace WslQuery
{
    [Flags, Serializable]
    public enum WslDistributionFlags
    {
        None = 0x0,
        EnableInterop = 0x1,
        AppendNtPath = 0x2,
        EnableDriveMouting = 0x4,
    }
}
