using Microsoft.EntityFrameworkCore;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.BaseEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Entities
{
    [Index(nameof(Code), IsUnique = true)]
    public class Permission : ReadonlyObject<int>
    {
        [SoftDisplayName]
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string NameLatin { get; set; }

        [StringLength(400)]
        public string Description { get; set; }

        [StringLength(400)]
        public string DescriptionLatin { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        public virtual List<Role> Roles { get; set; }
    }
}
