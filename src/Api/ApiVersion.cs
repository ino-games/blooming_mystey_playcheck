using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Service.LogicCommon
{
    class ApiVersion
    {
        private static string VersionString;

        public static string GetVersion()
        {
            if (VersionString == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("Service.LogicCommon.package.json"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    var package = JsonConvert.DeserializeObject<PackageDef>(reader.ReadToEnd());
                    VersionString = package.version;
                }
            }

            return VersionString;
        }

        private class PackageDef
        {
            public string version = "0.0.1";
        }
    }
}