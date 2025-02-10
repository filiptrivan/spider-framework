using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    public partial class RoleSaveBodyDTO
    {
        public List<long> SelectedUsersIds { get; set; }
    }
}
