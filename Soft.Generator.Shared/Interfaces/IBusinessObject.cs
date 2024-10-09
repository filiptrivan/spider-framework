using System.ComponentModel.DataAnnotations;

namespace Soft.Generator.Shared.Interfaces
{
    public interface IBusinessObject<T>
    {
        public T Id { get; }
        public int Version { get; }

        public DateTime CreatedAt { get; }
        public DateTime ModifiedAt { get; }
    }
}
