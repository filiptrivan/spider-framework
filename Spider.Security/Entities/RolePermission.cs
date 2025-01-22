using Spider.Shared.Attributes.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.Entities
{
    public class RolePermission
    {
        [M2MMaintanceEntity(nameof(Role.Permissions))]
        public virtual Role Role { get; set; }

        [M2MExtendEntity(nameof(Permission.Roles))]
        public virtual Permission Permission { get; set; }
    }
}
