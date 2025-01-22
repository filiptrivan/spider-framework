using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.DTO
{
    /// <summary>
    /// FT: For now, we only used this for the basic 2 property (or different one to many associations) M2M associations. 
    /// We should consider using this DTO also for > 2 properties M2M associations
    /// </summary>
    public class LazyLoadSelectedIdsResultDTO<ID> where ID : struct
    {
        public List<ID> SelectedIds { get; set; }
        public int TotalRecordsSelected { get; set; }
    }
}
