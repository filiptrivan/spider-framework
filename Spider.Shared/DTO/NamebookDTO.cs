using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.DTO
{
    public class NamebookDTO<T>
    {
        public T Id { get; set; }
        public string DisplayName { get; set; }
    }
}
