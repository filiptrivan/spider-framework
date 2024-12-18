using Soft.Generator.Shared.Attributes.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Entities
{
    public class RolePermission
    {
        public virtual Role Role { get; set; }

        [M2MMaintanceEntityKey(nameof(Role))]
        public int RoleId { get; set; }

        public virtual Permission Permission { get; set; }

        [M2MExtendEntityKey(nameof(Permission))]
        public int PermissionId { get; set; }
    }
}
