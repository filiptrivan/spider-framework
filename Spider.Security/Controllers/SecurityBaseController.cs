using Microsoft.AspNetCore.Mvc;
using Spider.Security.DTO;
using Spider.Security.Entities;
using Spider.Security.Interfaces;
using Spider.Security.Services;
using Spider.Shared.Attributes;
using Spider.Shared.Attributes.EF.UI;
using Spider.Shared.DTO;
using Spider.Shared.Helpers;
using Spider.Shared.Interfaces;

namespace Spider.Security.SecurityControllers // Needs to be other namespace because of source generator
{
    public class SecurityBaseController<TUser> : SpiderBaseController where TUser : class, IUser, new()
    {
        private readonly SecurityBusinessService<TUser> _securityBusinessService;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;
        private readonly AuthorizationService _authorizationService;

        public SecurityBaseController(
            SecurityBusinessService<TUser> securityBusinessService,
            IJwtAuthManager jwtAuthManagerService,
            IApplicationDbContext context,
            AuthenticationService authenticationService,
            AuthorizationService authorizationService
        )
        {
            _securityBusinessService = securityBusinessService;
            _jwtAuthManagerService = jwtAuthManagerService;
            _context = context;
            _authenticationService = authenticationService;
            _authorizationService = authorizationService;
        }

        #region Authentication

        [HttpPost]
        public async Task SendLoginVerificationEmail(LoginDTO loginDTO)
        {
            await _securityBusinessService.SendLoginVerificationEmail(loginDTO);
        }

        [HttpPost]
        public virtual async Task<AuthResultDTO> Login(VerificationTokenRequestDTO request)
        {
            return _securityBusinessService.Login(request);
        }

        [HttpPost]
        [UIDoNotGenerate]
        public virtual async Task<AuthResultDTO> LoginExternal(ExternalProviderDTO externalProviderDTO) // TODO FT: Add enum for which external provider you should login user
        {
            return await _securityBusinessService.LoginExternal(externalProviderDTO, SettingsProvider.Current.GoogleClientId);
        }

        [HttpPost]
        public async Task<RegistrationVerificationResultDTO> SendRegistrationVerificationEmail(RegistrationDTO registrationDTO)
        {
            return await _securityBusinessService.SendRegistrationVerificationEmail(registrationDTO);
        }

        [HttpPost]
        public virtual async Task<AuthResultDTO> Register(VerificationTokenRequestDTO request)
        {
            return await _securityBusinessService.Register(request);
        }

        [HttpGet]
        [AuthGuard]
        public ActionResult Logout(string browserId)
        {
            string email = _authenticationService.GetCurrentUserEmail();
            _jwtAuthManagerService.Logout(browserId, email); // FT: If the malicious user is deleting browser id, and sending request with refresh token like that we will delete every refresh token for that user

            return Ok();
        }

        /// <summary>
        /// Here we would put [Authorize] attribute, because we don't validate life time of the access token, but we are not because deeper in the method we are validating it without life time also. 
        /// </summary>
        [HttpPost]
        public async Task<AuthResultDTO> RefreshToken(RefreshTokenRequestDTO request)
        {
            return await _securityBusinessService.RefreshToken(request);
        }

        #endregion

        #region User

        [HttpGet]
        [AuthGuard]
        [SkipSpinner]
        public async Task<UserDTO> GetCurrentUser()
        {
            return await _securityBusinessService.GetCurrentUserDTO();
        }

        [HttpGet]
        [AuthGuard]
        [UIDoNotGenerate]
        public virtual async Task<List<string>> GetCurrentUserPermissionCodes()
        {
            return await _authorizationService.GetCurrentUserPermissionCodes<TUser>();
        }

        #endregion

        #region Role

        [HttpPost]
        [AuthGuard]
        public async Task<TableResponseDTO<RoleDTO>> GetRoleTableData(TableFilterDTO tableFilterDTO)
        {
            return await _securityBusinessService.GetRoleTableData(tableFilterDTO, _context.DbSet<Role>(), true);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportRoleTableDataToExcel(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _securityBusinessService.ExportRoleTableDataToExcel(tableFilterDTO, _context.DbSet<Role>(), true);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"Roles.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task DeleteRole(int id)
        {
            await _securityBusinessService.DeleteRole(id, true);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<RoleDTO> GetRole(int id)
        {
            return await _securityBusinessService.GetRoleDTO(id, true);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<RoleSaveBodyDTO> SaveRole(RoleSaveBodyDTO saveBodyDTO)
        {
            return await _securityBusinessService.SaveRoleAndReturnSaveBodyDTO(saveBodyDTO, true, true);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> GetUsersNamebookListForRole(int roleId)
        {
            return await _securityBusinessService.GetUsersNamebookListForRole(roleId);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetPermissionsDropdownListForRole()
        {
            return await _securityBusinessService.GetPermissionsDropdownListForRole(_context.DbSet<Permission>(), true);
        }

        [HttpGet]
        [AuthGuard]
        public virtual async Task<List<NamebookDTO<long>>> GetUsersAutocompleteListForRole(int limit, string query)
        {
            return await _securityBusinessService.GetUsersAutocompleteListForRole(limit, query, true);
        }

        #endregion

        #region Permission


        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetPermissionsNamebookListForRole(int roleId)
        {
            return await _securityBusinessService.GetPermissionsNamebookListForRole(roleId, true);
        }

        #endregion

    }
}
