using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using Soft.Generator.Shared.SoftExceptions;
using Soft.Generator.Security.Services;
using Microsoft.EntityFrameworkCore;
using Soft.Generator.Security.Entities;
using Soft.Generator.Infrastructure.Data;
using Soft.NgTable.Models;
using Soft.Generator.Shared.DTO;
using Google.Apis.Auth;
using Soft.Generator.Shared.Attributes;
using Soft.Generator.Shared.Helpers;

namespace Soft.Generator.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class AuthController : SoftControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly SecurityBusinessService _securityBusinessService;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly ApplicationDbContext _context;

        public AuthController(ILogger<AuthController> logger, SecurityBusinessService securityBusinessService, IJwtAuthManager jwtAuthManagerService, ApplicationDbContext context)
        {
            _logger = logger;
            _securityBusinessService = securityBusinessService;
            _jwtAuthManagerService = jwtAuthManagerService;
            _context = context;
        }

        #region Authentication

        [HttpPost]
        public async Task<LoginResultDTO> Login(LoginDTO loginDTO)
        {
            return await _securityBusinessService.Login(loginDTO, GetIPAddress(HttpContext));
        }

        [HttpPost]
        public async Task<LoginResultDTO> LoginExternal(ExternalProviderDTO externalProviderDTO) // TODO FT: Add enum for which external provider you should login user
        {
            return await _securityBusinessService.LoginExternal(externalProviderDTO, GetIPAddress(HttpContext), SettingsProvider.Current.GoogleClientId);
        }

        [HttpPost]
        public async Task<RegistrationResultDTO> Register(RegistrationDTO registrationDTO)
        {
            return await _securityBusinessService.Register(registrationDTO, GetIPAddress(HttpContext));
        }

        [HttpPost]
        // [AuthGuard] // FT: Without auth guard because we don't validate if access token expired.
        public async Task<LoginResultDTO> RegistrationVerification(VerificationTokenRequestDTO request)
        {
            return await _securityBusinessService.RegistrationVerification(request.VerificationToken, request.AccessToken, request.BrowserId, GetIPAddress(HttpContext));
        }

        [HttpPost]
        [AuthGuard]
        public ActionResult Logout()
        {
            string email = HttpContext.User.Identity?.Name!;
            _jwtAuthManagerService.RemoveRefreshTokenByEmail(email);

            return Ok();
        }

        /// <summary>
        /// Here we would put [Authorize] attribute, because we don't validate life time of the access token, but we are not because deeper in the method we are validating it without life time also. 
        /// </summary>
        [HttpPost]
        public async Task<LoginResultDTO> RefreshToken(RefreshTokenRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    throw new UnauthorizedException();

                string accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");

                return await _securityBusinessService.GetLoginResultDTOAsync(request, accessToken);
            }
            catch (Exception) // TODO FT: Im not sure if this is the right implementation, I need to log something also...
            {
                throw;
            }
        }

        #endregion

        #region User

        [HttpGet]
        [AuthGuard]
        public async Task<UserDTO> GetCurrentUser()
        {
            ClaimsIdentity identity = HttpContext.User.Identity as ClaimsIdentity;
            UserDTO userIdAndEmail = SecurityBusinessService.GetCurrentUserIdAndEmail(identity);
            return await _securityBusinessService.GetUserDTOAsync(userIdAndEmail.Id);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<BaseTableResponseEntity<UserDTO>> LoadUserListForTable(TableFilterDTO dto)
        {
            return await _securityBusinessService.LoadUserListForTable(dto);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportUserListToExcel(TableFilterDTO dto)
        {
            byte[] fileContent = await _securityBusinessService.ExportUserListToExcel(dto);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"Users.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task DeleteUser(long id)
        {
            await _securityBusinessService.DeleteEntity<User, long>(id);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<UserDTO> GetUser(long id)
        {
            return await _securityBusinessService.GetUserDTOAsync(id);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<UserDTO> SaveUser(UserDTO dto)
        {
            return await _securityBusinessService.SaveUserAndReturnDTOAsync(dto);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> LoadRoleListForAutocomplete(int limit, string query)
        {
            return await _securityBusinessService.LoadRoleListForAutocomplete(limit, query, _context.DbSet<Role>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> LoadRoleListForDropdown()
        {
            return await _securityBusinessService.LoadRoleListForDropdown(_context.DbSet<Role>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> LoadRoleListForUser(long userId)
        {
            return await _securityBusinessService.LoadRoleListForUser(userId);
        }

        #endregion

        #region Role

        [HttpPost]
        [AuthGuard]
        public async Task<BaseTableResponseEntity<RoleDTO>> LoadRoleListForTable(TableFilterDTO dto)
        {
            return await _securityBusinessService.LoadRoleListForTable(dto);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportRoleListToExcel(TableFilterDTO dto)
        {
            byte[] fileContent = await _securityBusinessService.ExportRoleListToExcel(dto);
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"Roles.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task DeleteRole(int id)
        {
            await _securityBusinessService.DeleteEntity<Role, int>(id);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<RoleDTO> GetRole(int id)
        {
            return await _securityBusinessService.GetRoleDTOAsync(id);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<RoleDTO> SaveRole(RoleDTO dto)
        {
            return await _securityBusinessService.SaveRoleAndReturnDTOAsync(dto);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> LoadUserListForAutocomplete(int limit, string query)
        {
            return await _securityBusinessService.LoadUserListForAutocomplete(limit, query, _context.DbSet<User>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> LoadUserListForDropdown()
        {
            return await _securityBusinessService.LoadUserListForDropdown(_context.DbSet<User>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> LoadUserListForRole(int roleId)
        {
            return await _securityBusinessService.LoadUserListForRole(roleId);
        }

        #endregion

        #region Permission

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> LoadPermissionListForDropdown()
        {
            return await _securityBusinessService.LoadPermissionListForDropdown(_context.DbSet<Permission>());
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<int>>> LoadPermissionListForRole(int roleId)
        {
            return await _securityBusinessService.LoadPermissionListForRole(roleId);
        }

        #endregion

    }
}


