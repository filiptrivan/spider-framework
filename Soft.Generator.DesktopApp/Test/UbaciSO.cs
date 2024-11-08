using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Test
{
    public class UbaciSO : OpstaSO
    {
        IOpstiDO _odo = null;

        public UbaciSO(IOpstiDO odo)
        {
            _odo = odo;
        }

        public override void OpstiIzvrsiSO()
        {
            _brokerBP.KreirajSlog(_odo);
        }
    }
}
