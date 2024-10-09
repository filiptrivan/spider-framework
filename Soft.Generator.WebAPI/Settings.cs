namespace Soft.Generator.WebAPI
{
    public static class SettingsProvider
    {
        public static Settings Current { internal get; set; } = new Settings();
    }

    public class Settings
    {
        public string ConnectionString { get; set; }
        public string FrontendUrl { get; set; }

        public string JwtKey { get; set; } // TODO FT: duplicated settings with Infrastructure ask VG OR BK how to solve that
        public string JwtIssuer { get; set; } // TODO FT: duplicated settings with Infrastructure ask VG OR BK how to solve that
        public string JwtAudience { get; set; } // TODO FT: duplicated settings with Infrastructure ask VG OR BK how to solve that
        public int ClockSkewMinutes { get; set; }

        public string GoogleClientId { get; set; }

        public string ExcelContentType { get; set; }
    }
}