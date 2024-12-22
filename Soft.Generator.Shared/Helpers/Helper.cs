using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Helpers
{
    public static class Helper
    {
        public static void WriteToTheFile(string data, string path)
        {
            StreamWriter sw = new StreamWriter(path);
            sw.WriteLine(data);
            sw.Close();
        }

        public static bool AreDatesEqualToSeconds(DateTime? date1, DateTime? date2)
        {
            if (!date1.HasValue && !date2.HasValue) return true; // Both null are considered equal
            if (!date1.HasValue || !date2.HasValue) return false; // One is null, and the other is not

            // Truncate both dates to seconds
            var truncatedDate1 = date1.Value.AddTicks(-(date1.Value.Ticks % TimeSpan.TicksPerSecond));
            var truncatedDate2 = date2.Value.AddTicks(-(date2.Value.Ticks % TimeSpan.TicksPerSecond));

            return truncatedDate1 == truncatedDate2;
        }

        public static T ReadAssemblyConfiguration<T>(string jsonConfigurationFile)
        {
            string name = typeof(T).Assembly.GetName().Name;
            string propertyName = "AppSettings";
            string text = ReadConfigFile(jsonConfigurationFile);
            if (string.IsNullOrEmpty(text))
            {
                return default(T);
            }

            foreach (JProperty item in JObject.Parse(text)[propertyName]!.Children().OfType<JProperty>())
            {
                if (item.Name == name)
                {
                    return item.Value.ToObject<T>();
                }
            }

            return default(T);
        }

        private static string ReadConfigFile(string jsonConfigurationFile)
        {
            using StreamReader streamReader = new StreamReader(jsonConfigurationFile);
            return streamReader.ReadToEnd();
        }
    }
}
