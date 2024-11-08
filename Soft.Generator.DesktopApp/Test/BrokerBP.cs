using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Soft.Generator.DesktopApp.Test
{
    public class BrokerBP
    {
        static SqlConnection conn;
        static SqlCommand comm;

        public bool KreirajSlog(IOpstiDO odo)
        {
            string command = "SELECT Max(" + odo.VratiAtributPretrazivanja()+ ") AS Max" +
            " FROM " + odo.VratiImeKlase();

            try
            {
                //st = conn.createStatement();
                //rs = st.executeQuery(upit);
                //if (rs.next() == false)
                //    odo.postaviPocetniBroj();
                //else
                //    odo.povecajBroj(rs);

                //upit = "INSERT INTO " + odo.vratiImeKlase() +
                //" VALUES (" + odo.vratiVrednostiAtributa() + ")";

                //st.executeUpdate(upit);
                //st.close();
            }
            catch (SqlException esql)
            {
                //porukaMetode = porukaMetode + "\nNe moze da se kreira novi slog: " + esql;
                return false;
            }

            //porukaMetode = porukaMetode + "\nKreiran je novi slog: ";
            return true;
        }
    }
}
