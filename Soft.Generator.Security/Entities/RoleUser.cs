using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Entities
{
    public class RoleUser
    {
        public int RolesId { get; set; }
        public long UsersId { get; set; }
    }
}
