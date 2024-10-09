using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Interfaces
{
    public interface IBusinessObjectDTO<T>
    {
        public T Id { get; }

        public int? Version { get; }
        public DateTime? CreatedAt { get; }
        public DateTime? ModifiedAt { get; }
    }
}
