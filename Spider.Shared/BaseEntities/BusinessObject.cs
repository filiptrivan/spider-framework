using Spider.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Spider.Shared.BaseEntities
{
    public class BusinessObject<T> : IBusinessObject<T> where T : struct
    {
        public T Id { get; private set; } // FT: Protected doesn't work with Mappster

        [Required]
        public int Version { get; private set; }

        [Required]
        public DateTime CreatedAt { get; private set; }

        [Required]
        public DateTime ModifiedAt { get; private set; }

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
