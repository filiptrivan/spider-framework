using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public partial class RoleSaveBodyDTO
    {
        //public RoleDTO RoleDTO { get; set; }
        public List<int> SelectedPermissionIds {  get; set; }
        public List<long> SelectedUserIds { get; set; }
    }
}
