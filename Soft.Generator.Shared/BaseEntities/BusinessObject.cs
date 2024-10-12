using Soft.Generator.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Soft.Generator.Shared.BaseEntities
{
    public class BusinessObject<T> : IBusinessObject<T> where T : struct
    {
        public T Id { get; protected set; }

        [Required]
        public int Version { get; protected set; }

        [Required]
        public DateTime CreatedAt { get; protected set; }

        [Required]
        public DateTime ModifiedAt { get; protected set; }

        public void SetVersion(int version)
        {
            Version = version;
        }

        public void SetCreatedAt(DateTime createdAt)
        {
            CreatedAt = createdAt;
        }

        public void SetModifiedAt(DateTime modifiedAt)
        {
            ModifiedAt = modifiedAt;
        }
    }
}
