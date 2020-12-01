using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using Wslhub.Sdk;

namespace WslQuery
{
    internal static class Program
    {
        [MTAThread]
        private static void Main(string[] args)
        {
            Wsl.InitializeSecurityModel();

            var hasPretty = string.Equals("--pretty", args?.FirstOrDefault(), StringComparison.OrdinalIgnoreCase);
            var queryResult = Wsl.GetDistroQueryResult();

            Console.Out.WriteLine(JsonConvert.SerializeObject(queryResult, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = hasPretty ? Formatting.Indented : Formatting.None,
            }));
        }
    }
}
