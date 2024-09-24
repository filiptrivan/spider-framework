using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.BaseEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Entities
{
    public class Notification : BusinessObject<long>
    {
        [SoftDisplayName]
        [StringLength(60, MinimumLength = 1)]
        [Required]
        public string Title { get; set; }

        [StringLength(60, MinimumLength = 1)]
        [Required]
        public string TitleLatin { get; set; }

        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string Description { get; set; }

        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string DescriptionLatin { get; set; }

    }
}
