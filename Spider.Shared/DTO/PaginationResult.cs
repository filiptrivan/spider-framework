using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.DTO
{
    public class PaginationResult<T> where T : class
    {
        public int TotalRecords { get; set; }
        public IQueryable<T> Query { get; set; }
    }
}
