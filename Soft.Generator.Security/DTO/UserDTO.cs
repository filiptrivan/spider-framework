using Soft.Generator.Shared.DTO;

namespace Soft.Generator.Security.DTO
{
    public partial class UserDTO
    {
        public string TestColumnForGrid { get; set; }
        public List<RoleDTO> Roles { get; set; }
    }
}
