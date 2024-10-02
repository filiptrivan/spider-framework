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
using Soft.Generator.Shared.SoftExceptions;
using Soft.NgTable.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.SecurityControllers // Needs to be other namespace because of source generator
{
    public class BaseSecurityController<TUser> : SoftControllerBase where TUser : class, IUser, new()
    {
        private readonly SecurityBusinessService _securityBusinessService;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;

        public BaseSecurityController(SecurityBusinessService securityBusinessService, IJwtAuthManager jwtAuthManagerService, IApplicationDbContext context, AuthenticationService authenticationService)
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
            await _securityBusinessService.SendLoginVerificationEmail<TUser>(loginDTO);
        }

        [HttpPost]
        public LoginResultDTO Login(VerificationTokenRequestDTO request)
        {
            return _securityBusinessService.Login(request);
        }


        [HttpPost]
        public async Task SendForgotPasswordVerificationEmail(ForgotPasswordDTO forgotPasswordDTO)
        {
            await _securityBusinessService.SendForgotPasswordVerificationEmail<TUser>(forgotPasswordDTO);
        }

        [HttpPost]
        public async Task<LoginResultDTO> ForgotPassword(VerificationTokenRequestDTO request)
        {
            return await _securityBusinessService.ForgotPassword<TUser>(request);
        }

        [HttpPost]
        public async Task<LoginResultDTO> LoginExternal(ExternalProviderDTO externalProviderDTO) // TODO FT: Add enum for which external provider you should login user
        {
            return await _securityBusinessService.LoginExternal<TUser>(externalProviderDTO, SettingsProvider.Current.GoogleClientId);
        }

        [HttpPost]
        public async Task<RegistrationVerificationResultDTO> SendRegistrationVerificationEmail(RegistrationDTO registrationDTO)
        {
            return await _securityBusinessService.SendRegistrationVerificationEmail<TUser>(registrationDTO);
        }

        [HttpPost]
        public async Task<LoginResultDTO> Register(VerificationTokenRequestDTO request)
        {
            return await _securityBusinessService.Register<TUser>(request);
        }

        [HttpGet]
        [AuthGuard]
        public ActionResult Logout(string browserId)
        {
            string email = _authenticationService.GetCurrentUserEmail();
            _jwtAuthManagerService.RemoveTheLastRefreshTokenFromTheSameBrowserAndEmail(browserId, email);

            return Ok();
        }

        /// <summary>
        /// Here we would put [Authorize] attribute, because we don't validate life time of the access token, but we are not because deeper in the method we are validating it without life time also. 
        /// </summary>
        [HttpPost]
        public async Task<LoginResultDTO> RefreshToken(RefreshTokenRequestDTO request)
        {
            return await _securityBusinessService.GetLoginResultDTOAsync<TUser>(request);
        }

        #endregion


        #region User

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
        public async Task<List<NamebookDTO<int>>> LoadRoleNamebookListForUserExtended(long userId)
        {
            return await _securityBusinessService.LoadRoleNamebookListForUserExtended<TUser>(userId);
        }

        #endregion


        #region Role

        [HttpPost]
        [AuthGuard]
        public async Task<BaseTableResponseEntity<RoleDTO>> LoadRoleListForTable(TableFilterDTO dto)
        {
            return await _securityBusinessService.LoadRoleListForTable(dto, _context.DbSet<Role>());
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportRoleListToExcel(TableFilterDTO dto)
        {
            byte[] fileContent = await _securityBusinessService.ExportRoleListToExcel(dto, _context.DbSet<Role>());
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
        public async Task<RoleDTO> SaveRole(RoleSaveBodyDTO roleSaveBodyDTO)
        {
            return await _securityBusinessService.SaveRoleAndReturnDTOExtendedAsync<TUser>(roleSaveBodyDTO);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> LoadUserListForRole(int roleId)
        {
            return await _securityBusinessService.LoadUserExtendedNamebookListForRole<TUser>(roleId);
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
            return await _securityBusinessService.LoadPermissionNamebookListForRole(roleId);
        }

        #endregion


        #region Notification

        [HttpPost]
        [AuthGuard]
        public async Task<BaseTableResponseEntity<NotificationDTO>> LoadNotificationListForTable(TableFilterDTO dto)
        {
            return await _securityBusinessService.LoadNotificationListForTable(dto, _context.DbSet<Notification>());
        }

        [HttpPost]
        [AuthGuard]
        public async Task<IActionResult> ExportNotificationListToExcel(TableFilterDTO dto)
        {
            byte[] fileContent = await _securityBusinessService.ExportNotificationListToExcel(dto, _context.DbSet<Notification>());
            return File(fileContent, SettingsProvider.Current.ExcelContentType, Uri.EscapeDataString($"Notifications.xlsx"));
        }

        [HttpDelete]
        [AuthGuard]
        public async Task DeleteNotification(long id)
        {
            await _securityBusinessService.DeleteEntity<Notification, long>(id);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<NotificationDTO> GetNotification(long id)
        {
            return await _securityBusinessService.GetNotificationDTOAsync(id);
        }

        [HttpPut]
        [AuthGuard]
        public async Task<NotificationDTO> SaveNotification(NotificationSaveBodyDTO notificationSaveBodyDTO)
        {
            return await _securityBusinessService.SaveNotificationAndReturnDTOExtendedAsync<TUser>(notificationSaveBodyDTO);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<List<NamebookDTO<long>>> LoadUserExtendedNamebookListForNotification(long notificationId)
        {
            return await _securityBusinessService.LoadUserExtendedNamebookListForNotification<TUser>(notificationId);
        }

        [HttpPost]
        [AuthGuard]
        public async Task<BaseTableResponseEntity<NotificationDTO>> LoadNotificationListForTheCurrentUser(TableFilterDTO tableFilterDTO)
        {
            return await _securityBusinessService.LoadNotificationListForTheCurrentUser<TUser>(tableFilterDTO);
        }

        [HttpGet]
        [AuthGuard]
        public async Task<int> GetUnreadNotificationCountForTheCurrentUser()
        {
            return await _securityBusinessService.GetUnreadNotificationCountForTheCurrentUser();
        }

        #endregion

    }
}
