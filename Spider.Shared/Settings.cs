using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared
{
    public static class SettingsProvider
    {
        public static Settings Current { internal get; set; } = new Settings();
    }

    public class Settings
    {
        public string ConnectionString { get; set; }


        public string EmailSender { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }


        public string JwtKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public int ClockSkewMinutes { get; set; }
        public string FrontendUrl { get; set; }


        public string BlobStorageConnectionString { get; set; }
        public string BlobStorageUrl { get; set; }
        public string BlobStorageContainerName { get; set; }
    }
}