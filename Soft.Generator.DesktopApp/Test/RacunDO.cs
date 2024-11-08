using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Test
{
    public class RacunDO : IOpstiDO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        public void PovecajBrojRacuna(int broj)
        {
            UbaciSO ubaciSO = new UbaciSO();

            ubaciSO.
        }

        public string VratiVrednostiAtributa()
        {
            return "1, Kosilica, 2000";
        }

        public string PostaviVrednostiAtributa()
        {
            return "";
        }

        public string VratiImeKlase()
        {
            return nameof(RacunDO);
        }

        public string VratiUslovZaNadjiSlog()
        {
            return "";
        }

        public string VratiUslovZaNadjiSlogove()
        {
            return "";
        }

        public string VratiAtributPretrazivanja()
        {
            return "Id";
        }
    }
}
