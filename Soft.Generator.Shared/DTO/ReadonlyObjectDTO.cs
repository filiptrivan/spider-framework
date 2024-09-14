using Soft.Generator.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.DTO
{
    public class ReadonlyObjectDTO<T> : IReadonlyObjectDTO<T>
    {
        public T Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
