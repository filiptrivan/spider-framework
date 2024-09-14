using Soft.Generator.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.BaseEntities
{
    public class ReadonlyObject<T> : IReadonlyObject<T>
    {
        public T Id { get; protected set; }

        [Required]
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// Don't use the method ever, its made just for automatic change from application db context
        /// </summary>
        public void SetCreatedAt(DateTime createdAt)
        {
            CreatedAt = createdAt;
        }
    }
}
