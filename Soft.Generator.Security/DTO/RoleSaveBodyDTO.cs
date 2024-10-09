using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class RoleSaveBodyDTO
    {
        public List<int> SelectedPermissionIds {  get; set; }
        public List<long> SelectedUserIds { get; set; }
        public RoleDTO RoleDTO { get; set; }
    }
}
