using Soft.Generator.Shared.DTO;

namespace Soft.Generator.Security.DTO // FT: Don't change namespace in generator, it's mandatory for partial classes
{
    public partial class UserDTO : BusinessObjectDTO<long>
    {
        public string Email { get; set; }
		public string Password { get; set; }
		public bool? HasLoggedInWithExternalProvider { get; set; }
		public int? NumberOfFailedAttemptsInARow { get; set; }
		public bool? IsVerified { get; set; }
    }
    public partial class RoleDTO : BusinessObjectDTO<int>
    {
        public string Name { get; set; }
		public string Description { get; set; }
    }
    public partial class PermissionDTO : ReadonlyObjectDTO<int>
    {
        public string Name { get; set; }
		public string Description { get; set; }
    }
}

