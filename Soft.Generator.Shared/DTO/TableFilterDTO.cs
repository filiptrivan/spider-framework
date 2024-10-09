using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.DTO
{
    public class TableFilterDTO
    {
        public Dictionary<string, List<TableFilterContext>> Filters { get; set; }
        public int First { get; set; }
        public int Rows { get; set; }
        public string SortField { get; set; }
        public int SortOrder { get; set; }
        public List<TableFilterSortMeta> MultiSortMeta { get; set; }
        public int? AdditionalFilterIdInt { get; set; }
        public long? AdditionalFilterIdLong { get; set; }
    }
}
