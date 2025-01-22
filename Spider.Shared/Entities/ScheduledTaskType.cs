using Spider.Shared.BaseEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Entities
{
    [NotMapped]
    public class ScheduledTaskType : ReadonlyObject<int>
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; } // TODO FT: Maybe put Code also inside ReadonlyObject?
    }
}
