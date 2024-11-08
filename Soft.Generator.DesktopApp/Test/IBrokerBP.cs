using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Test
{
    public interface IBrokerBP
    {
        public void OtvoriKonekciju();
        public void ZatvoriKonekciju();
        public void OtvoriTransakciju();
        public void ZatvoriTransakciju();
        public bool KonekcijaJeOtvorena();
        public bool KonekcijaJeZatvorena();
        public bool TransakcijaJeOtvorena();
        public bool TransakcijaJeZatvorena();
        public void UbaciSlog();
        public void ObrisiSlog();
        public void PromeniSlog();
        public void ProcitajSlog();
    }
}
