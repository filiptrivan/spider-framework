using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.NgTable.Models
{
    public class BasePaginationResult<T>
    {
        public int TotalRecords { get; set; }
        public IQueryable<T> Query { get; set; }
    }
}
