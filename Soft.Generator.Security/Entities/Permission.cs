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
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string NameLatin { get; set; }

        [StringLength(400, MinimumLength = 1)]
        public string Description { get; set; }

        [StringLength(400, MinimumLength = 1)]
        public string DescriptionLatin { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; } // TODO FT: Maybe put Code also inside ReadonlyObject?

        public virtual List<Role> Roles { get; set; }
    }
}
