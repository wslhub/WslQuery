using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.Json;
using WslQuery;

var tests = new List<(string Name, Action Run)>
{
    ("Legacy JSON field names and numeric values", JsonContract),
    ("Pretty JSON preserves values and nulls", PrettyJson),
    ("Empty results remain a JSON array", () => Equal("[]", QueryJson.Serialize([], false))),
    ("Help skips the native query", Help),
    ("Unknown arguments fail without echoing terminal controls", UnknownArgument),
    ("Case-insensitive pretty option", PrettyOption),
    ("Query failure uses stderr and a nonzero exit code", QueryFailure),
    ("Partial query failure preserves JSON and returns failure", PartialFailure),
    ("Unregistered distribution does not report success", Unregistered),
    ("Native UTF-8 environment array", NativeStrings),
    ("Empty native array", () => Equal(0, NativeEnvironment.ReadAndFree(0, 0).Count)),
    ("Invalid native array fails safely", InvalidNativeArray)
};
if (OperatingSystem.IsWindows())
    tests.Add(("Registry enumeration with isolated Windows fixtures", RegistryEnumeration));
else
    Console.WriteLine("SKIP: Windows registry fixtures require Windows.");

var failed = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS: {name}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL: {name}: {ex}"); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} tests passed.");
return failed == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}.");
}

static DistroInfo Sample() => new()
{
    DistroId = Guid.Parse("d596e687-441e-4478-a793-b51d46a3b847"),
    DistroName = "Ubuntu-한국어\"\\\n",
    BasePath = @"C:\Users\Example\Distro",
    DefaultEnvironmentVariables = ["LANG=ko_KR.UTF-8", "VALUE=<>&\""],
    KernelCommandLine = ["quiet"],
    IsRegistered = true,
    IsDefault = true,
    WslVersion = 2,
    DefaultUid = uint.MaxValue,
    DistroFlags = 7
};

static void JsonContract()
{
    var json = QueryJson.Serialize([Sample()], false);
    Equal(false, json.Contains('\n'));
    using var document = JsonDocument.Parse(json);
    var value = document.RootElement[0];
    // Golden field list comes from Wslhub.Sdk 0.1.2 and Newtonsoft's camel-case resolver.
    var expected = "appendNtPath,basePath,defaultEnvironmentVariables,defaultUid,distroFlags,distroId,distroName,enableDriveMounting,enableInterop,hResult,isDefault,isDefaultDistro,isRegistered,kernelCommandLine,succeed,wslVersion";
    Equal(expected, string.Join(",", value.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)));
    Equal(uint.MaxValue, value.GetProperty("defaultUid").GetUInt32());
    Equal(7, value.GetProperty("distroFlags").GetInt32());
    Equal(0, value.GetProperty("hResult").GetInt32());
    Equal(Sample().DistroName, value.GetProperty("distroName").GetString());
    foreach (var name in new[] { "isDefault", "isDefaultDistro", "isRegistered", "succeed", "enableInterop", "enableDriveMounting", "appendNtPath" })
        Equal(true, value.GetProperty(name).GetBoolean());
}

static void PrettyJson()
{
    var sample = Sample();
    sample.BasePath = null;
    var compact = QueryJson.Serialize([sample], false);
    var pretty = QueryJson.Serialize([sample], true);
    Equal(true, pretty.Contains('\n'));
    using var first = JsonDocument.Parse(compact);
    using var second = JsonDocument.Parse(pretty);
    Equal(true, JsonElement.DeepEquals(first.RootElement, second.RootElement));
    Equal(JsonValueKind.Null, second.RootElement[0].GetProperty("basePath").ValueKind);
}

static void Help()
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    Equal(0, CommandLine.Run(["--help"], output, error, () => throw new Exception("Query must not run")));
    Equal(CommandLine.Usage + Environment.NewLine, output.ToString());
    Equal("", error.ToString());
}

static void UnknownArgument()
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    Equal(2, CommandLine.Run(["--help", "\u001b[2J"], output, error, () => throw new Exception("Query must not run")));
    Equal("", output.ToString());
    Equal(false, error.ToString().Contains('\u001b'));
}

static void PrettyOption()
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    Equal(0, CommandLine.Run(["--PRETTY"], output, error, () => [Sample()]));
    Equal(true, output.ToString().Contains("  {"));
    Equal("", error.ToString());
}

static void QueryFailure()
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    Equal(1, CommandLine.Run([], output, error, () => throw new PlatformNotSupportedException("Test failure")));
    Equal("", output.ToString());
    Equal("WslQuery: Test failure" + Environment.NewLine, error.ToString());
}

static void PartialFailure()
{
    var failure = Sample();
    failure.HResult = unchecked((int)0x80070005);
    using var output = new StringWriter();
    using var error = new StringWriter();
    Equal(1, CommandLine.Run([], output, error, () => [Sample(), failure]));
    using var json = JsonDocument.Parse(output.ToString());
    Equal(2, json.RootElement.GetArrayLength());
    Equal(false, json.RootElement[1].GetProperty("succeed").GetBoolean());
    Equal(failure.HResult, json.RootElement[1].GetProperty("hResult").GetInt32());
    Equal(true, error.ToString().Contains("hResult"));
}

static void Unregistered()
{
    var sample = Sample();
    sample.IsRegistered = false;
    Equal(false, sample.Succeed);
}

static void NativeStrings()
{
    var array = Marshal.AllocCoTaskMem(2 * IntPtr.Size);
    Marshal.WriteIntPtr(array, 0, Marshal.StringToCoTaskMemUTF8("LANG=한국어"));
    Marshal.WriteIntPtr(array, IntPtr.Size, Marshal.StringToCoTaskMemUTF8("EMPTY="));
    // ReadAndFree owns both strings and the array from this point onwards.
    var result = NativeEnvironment.ReadAndFree(array, 2);
    Equal(2, result.Count);
    Equal("LANG=한국어", result[0]);
    Equal("EMPTY=", result[1]);
}

static void InvalidNativeArray()
{
    try { NativeEnvironment.ReadAndFree(0, 1); }
    catch (InvalidDataException) { return; }
    throw new Exception("Expected InvalidDataException");
}

static void RegistryEnumeration()
{
    if (!OperatingSystem.IsWindows())
        throw new PlatformNotSupportedException();
    Equal(0, WindowsDistroQuery.ReadRegistry(null).Count);
    var path = @"Software\WslQuery.Tests\" + Guid.NewGuid().ToString("N");
    using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
    try
    {
        using var root = currentUser.CreateSubKey(path);
        Equal(0, WindowsDistroQuery.ReadRegistry(root).Count);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        root.SetValue("DefaultDistribution", firstId.ToString("B"));
        using (var first = root.CreateSubKey(firstId.ToString("B")))
        {
            first.SetValue("DistributionName", "Ubuntu");
            first.SetValue("BasePath", @"C:\Fixtures\Ubuntu");
            first.SetValue("KernelCommandLine", "quiet\t debug  ");
        }
        using (var second = root.CreateSubKey(secondId.ToString("B")))
            second.SetValue("DistributionName", "Debian"); // Missing optional values are valid.
        using (root.CreateSubKey("not-a-distribution")) { }
        using (var invalid = root.CreateSubKey(Guid.NewGuid().ToString("B")))
            invalid.SetValue("DistributionName", "  ");
        var results = WindowsDistroQuery.ReadRegistry(root);
        Equal(2, results.Count);
        Equal(1, results.Count(distro => distro.IsDefault && distro.IsDefaultDistro));
        Equal("quiet,debug", string.Join(",", results.Single(distro => distro.DistroId == firstId).KernelCommandLine));
        Equal<string?>(null, results.Single(distro => distro.DistroId == secondId).BasePath);
        Equal(true, results.All(distro => !distro.Succeed));
        using (var emptyId = root.CreateSubKey(Guid.Empty.ToString("B")))
            emptyId.SetValue("DistributionName", "EmptyGuidFixture");
        root.SetValue("DefaultDistribution", "malformed");
        Equal(0, WindowsDistroQuery.ReadRegistry(root).Count(distro => distro.IsDefault));
    }
    finally
    {
        currentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
    }
}
