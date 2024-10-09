using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.DTO
{
    public class TableResponseDTO<T> where T : class
    {
        public IList<T> Data { get; set; }
        public int TotalRecords { get; set; }
    }
}
