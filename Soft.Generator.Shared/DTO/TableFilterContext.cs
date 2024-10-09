using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.DTO
{
    public class TableFilterContext
    {
        public object Value { get; set; }
        public string MatchMode { get; set; }
        public string Operator { get; set; }
    }
}
