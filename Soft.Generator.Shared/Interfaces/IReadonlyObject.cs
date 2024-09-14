using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Interfaces
{
    public interface IReadonlyObject<T>
    {
        public T Id { get; }
        public DateTime CreatedAt { get; }

    }
}
