using Spider.Security.Interface;
using Spider.Shared.Attributes.EF;
using Spider.Shared.Attributes.EF.UI;
using Spider.Shared.BaseEntities;
using Spider.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.Entities
{
    public class Role : BusinessObject<int>
    {
        [DisplayName]
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; }

        [StringLength(400, MinimumLength = 1)]
        public string Description { get; set; }

        //[UIControlType(nameof(UIControlTypeCodes.MultiAutocomplete))]
        //public virtual List<TUser> Users { get; set; }

        [UIControlType(nameof(UIControlTypeCodes.MultiSelect))]
        public virtual List<Permission> Permissions { get; } = new();
    }
}