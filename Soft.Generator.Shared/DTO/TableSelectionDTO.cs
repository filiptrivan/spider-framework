using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.DTO
{
    public class TableSelectionDTO<T> where T : struct
    {
        public TableFilterDTO TableFilter { get; set; }
        public List<T> SelectedIds { get; set; }
        public List<T> UnselectedIds { get; set; }
        public bool? IsAllSelected { get; set; }
    }
}
