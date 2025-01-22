using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Infrastructure
{
    public static class SettingsProvider
    {
        public static Settings Current { internal get; set; } = new Settings();
    }

    public class Settings
    {
        public bool UseGoogleAsExternalProvider { get; set; }
        public bool AppHasLatinTranslation { get; set; }
    }
}
