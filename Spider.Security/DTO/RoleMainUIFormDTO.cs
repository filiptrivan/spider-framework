using Spider.Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.DTO
{
    public partial class RoleMainUIFormDTO
    {
        public List<NamebookDTO<long>> UsersNamebookDTOList { get; set; }
    }
}
