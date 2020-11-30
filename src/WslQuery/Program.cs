using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Wslhub.Sdk;

namespace WslQuery
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var hasPretty = string.Equals("--pretty", args?.FirstOrDefault(), StringComparison.OrdinalIgnoreCase);
            var queryResult = Wsl.GetDistroQueryResult();

            Console.Out.WriteLine(JsonConvert.SerializeObject(queryResult, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = hasPretty ? Formatting.Indented : Formatting.None,
            }));
        }

        private static IEnumerable<string> ExecuteAndGetResult(string executablePath, string commandLineArguments)
        {
            var processStartInfo = new ProcessStartInfo(executablePath, commandLineArguments)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                LoadUserProfile = true,
#pragma warning restore CA1416 // Validate platform compatibility
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
