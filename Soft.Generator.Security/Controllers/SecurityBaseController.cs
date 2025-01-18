using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Entities;
using Soft.Generator.Security.Interface;
using Soft.Generator.Security.Services;
using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.DTO;
using Soft.Generator.Shared.Helpers;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.Extensions;
using Soft.Generator.Shared.SoftExceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Soft.Generator.Security.SecurityControllers // Needs to be other namespace because of source generator
{
    public class SecurityBaseController<TUser> : SoftBaseController where TUser : class, IUser, new()
    {
        private readonly SecurityBusinessService<TUser> _securityBusinessService;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;

        public SecurityBaseController(SecurityBusinessService<TUser> securityBusinessService, IJwtAuthManager jwtAuthManagerService, IApplicationDbContext context, AuthenticationService authenticationService)
        {
            _securityBusinessService = securityBusinessService;
            _jwtAuthManagerService = jwtAuthManagerService;
            _context = context;
            _authenticationService = authenticationService;
        }

        #region Authentication

        [HttpPost]
        public async Task SendLoginVerificationEmail(LoginDTO loginDTO)
        {
            await _securityBusinessService.SendLoginVerificationEmail(loginDTO);
        }

        [HttpPost]
        public async Task<RegistrationVerificationResultDTO> SendRegistrationVerificationEmail(RegistrationDTO registrationDTO)
        {
            return await _securityBusinessService.SendRegistrationVerificationEmail(registrationDTO);
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

        #region Role

        [HttpPost]
        [AuthGuard]
        public async Task<TableResponseDTO<RoleDTO>> GetRoleTableData(TableFilterDTO tableFilterDTO)
        {
            return await _securityBusinessService.GetRoleTableData(tableFilterDTO, _context.DbSet<Role>());
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportRoleTableDataToExcel(TableFilterDTO tableFilterDTO)
        {
            byte[] fileContent = await _securityBusinessService.ExportRoleTableDataToExcel(tableFilterDTO, _context.DbSet<Role>());
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"Roles.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task DeleteRole(int id)
        {
            await _securityBusinessService.DeleteRoleAsync(id, true);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<RoleDTO> GetRole(int id)
        {
            return await _securityBusinessService.GetRoleDTOAsync(id);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<RoleDTO> SaveRole(RoleSaveBodyDTO roleSaveBodyDTO)
        {
            return await _securityBusinessService.SaveRoleAndReturnDTOExtendedAsync(roleSaveBodyDTO);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> GetUsersNamebookListForRole(int roleId)
        {
            return await _securityBusinessService.GetUsersNamebookListForRole(roleId);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetRoleListForAutocomplete(int limit, string query)
        {
            return await _securityBusinessService.GetRoleListForAutocomplete(limit, query, _context.DbSet<Role>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetRoleListForDropdown()
        {
            return await _securityBusinessService.GetRoleListForDropdown(_context.DbSet<Role>());
        }

        #endregion

        #region Permission

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetPermissionListForDropdown()
        {
            return await _securityBusinessService.GetPermissionListForDropdown(_context.DbSet<Permission>(), false); // FT: We don't have authorization of Permission, it will inherit from Role authorization
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> GetPermissionsNamebookListForRole(int roleId)
        {
            return await _securityBusinessService.GetPermissionsNamebookListForRole(roleId);
        }

        #endregion

    }
}
