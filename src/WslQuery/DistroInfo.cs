namespace WslQuery;

// Retains the Wslhub.Sdk 0.1.2 JSON contract, including numeric flags and HRESULTs.
internal sealed class DistroInfo
{
    public bool Succeed => IsRegistered && HResult == 0;
    public List<string> DefaultEnvironmentVariables { get; set; } = [];
    public uint DefaultUid { get; set; }
    public bool EnableInterop => (DistroFlags & 1) != 0;
    public bool EnableDriveMounting => (DistroFlags & 4) != 0;
    public bool AppendNtPath => (DistroFlags & 2) != 0;
    public uint DistroFlags { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsDefaultDistro => IsDefault;
    public int HResult { get; set; }
    public uint WslVersion { get; set; }
    public Guid DistroId { get; set; }
    public string DistroName { get; set; } = "";
    public List<string> KernelCommandLine { get; set; } = [];
    public string? BasePath { get; set; }
    public bool IsDefault { get; set; }
}
