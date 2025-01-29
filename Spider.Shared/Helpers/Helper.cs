using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Helpers
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

        #region IP Address

        public static string GetIPAddress(HttpContext httpContext)
        {
            string ipAddress = GetRemoteHostIpAddressUsingXForwardedFor(httpContext)?.ToString();

            if (string.IsNullOrEmpty(ipAddress))
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ipAddress))
                ipAddress = GetRemoteHostIpAddressUsingXRealIp(httpContext)?.ToString();

            return ipAddress;
        }

        private static IPAddress GetRemoteHostIpAddressUsingXForwardedFor(HttpContext httpContext)
        {
            IPAddress remoteIpAddress = null;
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(forwardedFor) == false)
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim());
                foreach (var ip in ips)
                {
                    if (IPAddress.TryParse(ip, out var address) &&
                        (address.AddressFamily is AddressFamily.InterNetwork
                         or AddressFamily.InterNetworkV6))
                    {
                        remoteIpAddress = address;
                        break;
                    }
                }
            }
            return remoteIpAddress;
        }

        private static IPAddress GetRemoteHostIpAddressUsingXRealIp(HttpContext httpContext)
        {
            IPAddress remoteIpAddress = null;
            var xRealIpExists = httpContext.Request.Headers.TryGetValue("X-Real-IP", out var xRealIp);
            if (xRealIpExists)
            {
                if (!IPAddress.TryParse(xRealIp, out IPAddress address))
                {
                    return remoteIpAddress;
                }
                var isValidIP = (address.AddressFamily is AddressFamily.InterNetwork
                                 or AddressFamily.InterNetworkV6);

                if (isValidIP)
                {
                    remoteIpAddress = address;
                }
                return remoteIpAddress;
            }
            return remoteIpAddress;
        }

        #endregion
    }
}
