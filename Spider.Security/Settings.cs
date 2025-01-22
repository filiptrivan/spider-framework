using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security
{
    public static class SettingsProvider
    {
        public static Settings Current { internal get; set; } = new Settings();
    }

    public class Settings
    {
        public string JwtKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public int ClockSkewMinutes { get; set; }
        public int AccessTokenExpiration { get; set; }
        public int RefreshTokenExpiration { get; set; }

        public string GoogleClientId { get; set; }

        /// <summary>
        /// It can be bigger, it has the same chance of being hit as the refresh token, but there is no reason why we would give it longer
        /// It is actually a modified refresh token
        /// </summary>
        public int VerificationTokenExpiration { get; set; } 
        public bool AllowTheUseOfAppWithDifferentIpAddresses { get; set; }
        public int AllowedBrowsersForTheSingleUser { get; set; }

        public string ExcelContentType { get; set; }
    }
}