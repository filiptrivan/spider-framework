using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.NgTable.Models
{
    public class BaseTableResponseEntity<T> where T : class
    {
        public IList<T> Data { get; set; }
        public int TotalRecords { get; set; }
    }
}
