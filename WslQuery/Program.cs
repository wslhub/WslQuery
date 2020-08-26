using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WslQuery
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var hasPretty = string.Equals("--pretty", args?.FirstOrDefault(), StringComparison.OrdinalIgnoreCase);
            var queryResult = GetDistroQueryResult();

            Console.Out.WriteLine(JsonConvert.SerializeObject(queryResult, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = hasPretty ? Formatting.Indented : Formatting.None,
            }));
        }

        private static DistroQueryResult GetDistroQueryResult()
        {
            var queryResult = new DistroQueryResult();

            try
            {
                AssertSystemStatus();

                foreach (var eachItem in GetWslDistroListFromRegistry())
                {
                    var distro = new DistroInfo()
                    {
                        DistroId = eachItem.DistroId,
                        DistroName = eachItem.DistroName,
                        BasePath = eachItem.BasePath,
                    };
                    distro.KernelCommandLine.AddRange(eachItem.KernelCommandLine);
                    queryResult.Distros.Add(distro);

                    distro.IsRegistered = NativeMethods.WslIsDistributionRegistered(eachItem.DistroName);

                    if (distro.IsRegistered)
                    {
                        distro.HResult = NativeMethods.WslGetDistributionConfiguration(
                            eachItem.DistroName,
                            out int distroVersion,
                            out int defaultUserId,
                            out WslDistributionFlags flags,
                            out IntPtr environmentVariables,
                            out int environmentVariableCount);

                        if (distro.Succeed)
                        {
                            distro.WslVersion = distroVersion;
                            distro.DefaultUid = defaultUserId;
                            distro.DistroFlags = flags;

                            unsafe
                            {
                                byte*** lpEnvironmentVariables = (byte***)environmentVariables.ToPointer();
                                for (int i = 0; i < environmentVariableCount; i++)
                                {
                                    byte** lpArray = lpEnvironmentVariables[i];
                                    var content = Marshal.PtrToStringAnsi(new IntPtr(lpArray));
                                    distro.DefaultEnvironmentVariables.Add(content);
                                    Marshal.FreeCoTaskMem(new IntPtr(lpArray));
                                }
                                Marshal.FreeCoTaskMem(new IntPtr(lpEnvironmentVariables));
                            }
                        }
                    }
                }

                var defaultDistroName = ExecuteAndGetResult(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wsl.exe"),
                    "--list --quiet")
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(defaultDistroName))
                {
                    var defaultDistro = queryResult.Distros
                        .Where(x => string.Equals(x.DistroName, defaultDistroName, StringComparison.Ordinal))
                        .FirstOrDefault();

                    if (defaultDistro != null)
                        defaultDistro.IsDefaultDistro = true;
                }

                queryResult.Succeed = true;
            }
            catch (Exception ex)
            {
                queryResult.Distros = null;
                queryResult.Succeed = false;
                queryResult.Error = ex.Message;
            }

            return queryResult;
        }

        private static IEnumerable<DistroRegistryInfo> GetWslDistroListFromRegistry()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using var lxssKey = currentUser.OpenSubKey(lxssPath, false);
            var results = new List<DistroRegistryInfo>();

            foreach (var keyName in lxssKey.GetSubKeyNames())
            {
                if (!Guid.TryParse(keyName, out Guid parsedGuid))
                    continue;

                using var distroKey = lxssKey.OpenSubKey(keyName);
                var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

                if (string.IsNullOrWhiteSpace(distroName))
                    continue;

                var basePath = distroKey.GetValue("BasePath", default(string)) as string;
                var normalizedPath = Path.GetFullPath(basePath);

                var kernelCommandLine = (distroKey.GetValue("KernelCommandLine", default(string)) as string ?? string.Empty);
                var result = new DistroRegistryInfo()
                {
                    DistroId = parsedGuid,
                    DistroName = distroName,
                    BasePath = basePath,
                };
                result.KernelCommandLine.AddRange(kernelCommandLine.Split(
                    new char[] { ' ', '\t', },
                    StringSplitOptions.RemoveEmptyEntries));
                results.Add(result);
            }

            return results;
        }

        private static IEnumerable<string> ExecuteAndGetResult(string executablePath, string commandLineArguments)
        {
            var processStartInfo = new ProcessStartInfo(executablePath, commandLineArguments)
            {
                LoadUserProfile = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode,
                CreateNoWindow = true,
            };

            using var process = new Process()
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            if (!process.Start())
                throw new Exception("Cannot start the WSL process.");

            return process.StandardOutput
                .ReadToEnd()
                .Split(new char[] { '\r', '\n', }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void AssertSystemStatus()
        {
            var commonErrorMessage = "This application requires 64-bit system and latest version of Windows 10 or higher than Windows Server 1709.";

            if (!Environment.Is64BitOperatingSystem || !Environment.Is64BitProcess)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var osVersion = Environment.OSVersion;

            if (osVersion.Platform != PlatformID.Win32NT)
                throw new PlatformNotSupportedException(commonErrorMessage);

            var versionNumber = osVersion.Version;

            if (versionNumber.Major < 10 ||
                versionNumber.Minor < 0 ||
                versionNumber.Build < 16299)
                throw new PlatformNotSupportedException(commonErrorMessage);

            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "wslapi.dll")))
                throw new NotSupportedException("This system does not have WSL enabled.");

            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "wsl.exe")))
                throw new NotSupportedException("This system does not have wsl.exe CLI.");
        }
    }
}
