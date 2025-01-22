using Spider.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.DTO
{
    public class BusinessObjectDTO<T> : IBusinessObjectDTO<T>
    {
        public T Id { get; set; }

        public int? Version { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
