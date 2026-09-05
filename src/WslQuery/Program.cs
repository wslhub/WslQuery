using System.Runtime.InteropServices;
using System.Text;

namespace WslQuery;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        return CommandLine.Run(args, Console.Out, Console.Error, Query);
    }

    private static List<DistroInfo> Query()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("WslQuery requires 64-bit Windows with WSL installed.");

        return WindowsDistroQuery.Query();
    }
}

internal static class CommandLine
{
    internal const string Usage = "Usage: WslQuery.exe [--pretty] [--help]";

    internal static int Run(string[] args, TextWriter output, TextWriter error, Func<List<DistroInfo>> query)
    {
        var pretty = false;
        var help = false;
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--pretty", StringComparison.OrdinalIgnoreCase))
                pretty = true;
            else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || arg == "-h")
                help = true;
            else
            {
                error.WriteLine("WslQuery: unknown argument.");
                error.WriteLine(Usage);
                return 2;
            }
        }

        if (help)
        {
            output.WriteLine(Usage);
            return 0;
        }

        try
        {
            var results = query();
            output.WriteLine(QueryJson.Serialize(results, pretty));
            if (results.Any(distro => !distro.Succeed))
            {
                error.WriteLine("WslQuery: some distributions could not be queried; inspect hResult in the JSON output.");
                return 1;
            }
            return 0;
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException or ExternalException
            or IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            error.WriteLine($"WslQuery: {ex.Message}");
            return 1;
        }
    }
}
