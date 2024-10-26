using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Entities
{
    public class Setting
    {
        public long Id { get; set; }

        public bool HasGoogleAuth { get; set; }

        public string PrimaryColor { get; set; }

        public bool HasLatinTranslate { get; set; }

        public bool HasDarkMode { get; set; }

        public bool HasNotifications { get; set; }

        public virtual Framework Framework { get; set; }

        public virtual List<Application> Applications { get; set; }
    }
}
