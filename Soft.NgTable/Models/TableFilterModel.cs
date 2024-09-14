using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Soft.NgTable.Models
{
    public class TableFilterModel
    {
        public Dictionary<string, object> Filters { get; set; }
        public int First { get; set; }
        public int Rows { get; set; }
        public string SortField { get; set; }
        public int SortOrder { get; set; }
        public List<TableFilterSortMeta> MultiSortMeta { get; set; }
    }
}
