using Microsoft.EntityFrameworkCore;
using Soft.Generator.Security.DataMappers;
using Soft.Generator.Security.DTO;
using System.Security.Claims;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.SoftExceptions;
using System.Linq.Dynamic.Core;
using Google.Apis.Auth;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.Extensions;
using Soft.Generator.Security.ValidationRules;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Soft.Generator.Shared.Emailing;
using Soft.Generator.Security.Enums;
using System.Net;
using Soft.Generator.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Soft.Generator.Security.Entities;
using Soft.Generator.Shared.DTO;
using Mapster;
using Azure.Storage.Blobs;
using Microsoft.IdentityModel.Tokens;
using Soft.Generator.Shared.Terms;
using Nucleus.Core.BusinessObject;

namespace Soft.Generator.Security.Services
{
    public class SecurityBusinessService<TUser> : SecurityBusinessServiceGenerated<TUser> where TUser : class, IUser, new()
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly AuthenticationService _authenticationService;
        private readonly AuthorizationBusinessService<TUser> _authorizationService;
        private readonly EmailingService _emailingService;
        private readonly BlobContainerClient _blobContainerClient;

        public SecurityBusinessService(IApplicationDbContext context, IJwtAuthManager jwtAuthManagerService, EmailingService emailingService, AuthenticationService authenticationService, AuthorizationBusinessService<TUser> authorizationService, 
            ExcelService excelService, BlobContainerClient blobContainerClient)
            : base(context, excelService, authorizationService, blobContainerClient)
        {
            _context = context;
            _jwtAuthManagerService = jwtAuthManagerService;
            _emailingService = emailingService;
            _authenticationService = authenticationService;
            _authorizationService = authorizationService;
            _blobContainerClient = blobContainerClient;
        }

        #region Authentication

        // Login
        public async Task SendLoginVerificationEmail(LoginDTO loginDTO)
        {
            LoginDTOValidationRules validationRules = new LoginDTOValidationRules();
            validationRules.ValidateAndThrow(loginDTO);

            string userEmail = null;
            long userId = 0;
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await Authenticate(loginDTO);
                userEmail = user.Email;
                userId = user.Id;
            });
            string verificationCode = _jwtAuthManagerService.GenerateAndSaveLoginVerificationCode(userEmail, userId, loginDTO.BrowserId);
            try
            {
                await _emailingService.SendVerificationEmailAsync(userEmail, verificationCode);
            }
            catch (Exception)
            {
                _jwtAuthManagerService.RemoveLoginVerificationTokensByEmail(userEmail); // We didn't send email, set all verification tokens invalid then
                throw;
            }
        }

        public LoginResultDTO Login(VerificationTokenRequestDTO verificationRequestDTO)
        {
            VerificationTokenRequestDTOValidationRules validationRules = new VerificationTokenRequestDTOValidationRules();
            validationRules.ValidateAndThrow(verificationRequestDTO);

            LoginVerificationTokenDTO loginVerificationTokenDTO = _jwtAuthManagerService.ValidateAndGetLoginVerificationTokenDTO(
                verificationRequestDTO.VerificationCode, verificationRequestDTO.BrowserId, verificationRequestDTO.Email); // FT: Can not be null, if its null it already has thrown
            // TODO FT: Log somewhere good and bad request
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(loginVerificationTokenDTO.UserId, loginVerificationTokenDTO.Email, loginVerificationTokenDTO.BrowserId);
            return GetLoginResultDTO(loginVerificationTokenDTO.UserId, loginVerificationTokenDTO.Email, jwtAuthResultDTO);
        }

        public async Task<LoginResultDTO> LoginExternal(ExternalProviderDTO externalProviderDTO, string googleClientId)
        {
            GoogleJsonWebSignature.Payload payload = await ValidateGoogleToken(externalProviderDTO.IdToken, googleClientId);

            return await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetUserByEmailAsync(payload.Email); // FT: Check if user already exist in the database
                DbSet<TUser> userDbSet = _context.DbSet<TUser>();

                if (user == null)
                {
                    user = new TUser
                    {
                        Email = payload.Email,
                        HasLoggedInWithExternalProvider = true,
                        NumberOfFailedAttemptsInARow = 0
                    };
                    await userDbSet.AddAsync(user);
                    await _context.SaveChangesAsync(); // Adding the new user which is logged in first time
                }
                else
                {
                    if (user.NumberOfFailedAttemptsInARow > SettingsProvider.Current.NumberOfFailedLoginAttemptsInARowToDisableUser)
                        throw new BusinessException(SharedTerms.DisabledAccountException); // TODO FT: We will let this for now, because now there is no other way to make the user can't login

                    if (user.HasLoggedInWithExternalProvider == false)
                        await userDbSet.ExecuteUpdateAsync(x => x.SetProperty(x => x.HasLoggedInWithExternalProvider, true)); // There is no need for SaveChangesAsync because we don't need to update the version of the user
                }

                JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(user.Id, user.Email, externalProviderDTO.BrowserId);

                return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
            });
        }


        // Forgot password
        public async Task SendForgotPasswordVerificationEmail(ForgotPasswordDTO forgotPasswordDTO)
        {
            ForgotPasswordDTOValidationRules validationRules = new ForgotPasswordDTOValidationRules();
            validationRules.ValidateAndThrow(forgotPasswordDTO);

            string userEmail = null;
            long userId = 0;

            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetUserByEmailAsync(forgotPasswordDTO.Email);

                if (user == null)
                    throw new BusinessException(SharedTerms.ResetPasswordEmailDoesNotExistException);

                userEmail = user.Email;
                userId = user.Id;
            });

            string verificationCode = _jwtAuthManagerService.GenerateAndSaveForgotPasswordVerificationCode(userEmail, userId, forgotPasswordDTO.NewPassword, forgotPasswordDTO.BrowserId);

            try
            {
                await _emailingService.SendVerificationEmailAsync(userEmail, verificationCode);
            }
            catch (Exception)
            {
                _jwtAuthManagerService.RemoveForgotPasswordVerificationTokensByEmail(userEmail); // We didn't send email, set all verification tokens invalid then
                throw;
            }
        }

        public async Task<LoginResultDTO> ForgotPassword(VerificationTokenRequestDTO verificationRequestDTO)
        {
            VerificationTokenRequestDTOValidationRules validationRules = new VerificationTokenRequestDTOValidationRules();
            validationRules.ValidateAndThrow(verificationRequestDTO);

            ForgotPasswordVerificationTokenDTO forgotPasswordVerificationTokenDTO = _jwtAuthManagerService.ValidateAndGetForgotPasswordVerificationTokenDTO(
                verificationRequestDTO.VerificationCode, verificationRequestDTO.BrowserId, verificationRequestDTO.Email); // FT: Can not be null, if its null it already has thrown
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await LoadInstanceAsync<TUser, long>(forgotPasswordVerificationTokenDTO.UserId, null);
                user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(forgotPasswordVerificationTokenDTO.NewPassword);
                _context.DbSet<TUser>().Update(user);
                await _context.SaveChangesAsync();
            });
            // TODO FT: Log somewhere good and bad request
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(forgotPasswordVerificationTokenDTO.UserId, forgotPasswordVerificationTokenDTO.Email, forgotPasswordVerificationTokenDTO.BrowserId);
            return GetLoginResultDTO(forgotPasswordVerificationTokenDTO.UserId, forgotPasswordVerificationTokenDTO.Email, jwtAuthResultDTO);
        }

        // Registration
        public async Task<RegistrationVerificationResultDTO> SendRegistrationVerificationEmail(RegistrationDTO registrationDTO)
        {
            RegistrationVerificationResultDTO registrationResultDTO = new RegistrationVerificationResultDTO();

            RegistrationDTOValidationRules validationRules = new RegistrationDTOValidationRules();
            validationRules.ValidateAndThrow(registrationDTO);

            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetUserByEmailAsync(registrationDTO.Email);

                if (user == null)
                {
                    string verificationCode = _jwtAuthManagerService.GenerateAndSaveRegistrationVerificationCode(registrationDTO.Email, registrationDTO.Password, registrationDTO.BrowserId);

                    try
                    {
                        await _emailingService.SendVerificationEmailAsync(registrationDTO.Email, verificationCode);
                    }
                    catch (Exception)
                    {
                        _jwtAuthManagerService.RemoveRegistrationVerificationTokensByEmail(registrationDTO.Email); // We didn't send email, set all verification tokens invalid then
                        throw;
                    }
                }
                else if (user.HasLoggedInWithExternalProvider && user.Password == null)
                {
                    throw new BusinessException(SharedTerms.OnlyThirdPartyAccountButTriedToLoginException);
                }
                else if (user.Password != null)
                {
                    throw new BusinessException(SharedTerms.SameEmailAlreadyExistsException);
                }
            });

            return registrationResultDTO;
        }

        public async Task<LoginResultDTO> Register(VerificationTokenRequestDTO verificationRequestDTO)
        {
            VerificationTokenRequestDTOValidationRules validationRules = new VerificationTokenRequestDTOValidationRules();
            validationRules.ValidateAndThrow(verificationRequestDTO);

            RegistrationVerificationTokenDTO registrationVerificationTokenDTO = _jwtAuthManagerService.ValidateAndGetRegistrationVerificationTokenDTO(
                verificationRequestDTO.VerificationCode, verificationRequestDTO.BrowserId, verificationRequestDTO.Email); // FT: Can not be null, if its null it already has thrown
            TUser user = null;
            await _context.WithTransactionAsync(async () =>
            {
                user = new TUser
                {
                    Email = registrationVerificationTokenDTO.Email,
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword(registrationVerificationTokenDTO.Password),
                    HasLoggedInWithExternalProvider = false, // FT: He couldn't do this if already has account
                    NumberOfFailedAttemptsInARow = 0
                };
                await _context.DbSet<TUser>().AddAsync(user);
                await _context.SaveChangesAsync();
            });
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(user.Id, user.Email, verificationRequestDTO.BrowserId); // FT: User can't be null, it would throw earlier if he is
            //await SaveLoginAndReturnDomainAsync(loginDTO); // FT: Is ipAddress == null is checked here // TODO FT: Log it
            return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
        }


        public async Task<LoginResultDTO> RefreshToken(RefreshTokenRequestDTO refreshTokenRequestDTO)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenRequestDTO.RefreshToken))
                throw new SecurityTokenException(SharedTerms.ExpiredRefreshTokenException); // FT: It's not realy this reason, but it's easier then realy explaining the user what has happened, this could happen if he deleted the cache from the browser

            string accessToken = await _authenticationService.GetAccessTokenAsync();
            List<Claim> claims = _jwtAuthManagerService.GetClaimsForTheAccessToken(refreshTokenRequestDTO, accessToken);

            long accesTokenUserId = long.Parse(claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value);
            string accessTokenUserEmail = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

            string emailFromTheDb = await GetCurrentUserEmailByIdAsync(accesTokenUserId);
            if (emailFromTheDb != accessTokenUserEmail) // The email from db changed, and the user is using the old one in access token
                _jwtAuthManagerService.RemoveRefreshTokenByEmail(accessTokenUserEmail);

            JwtAuthResultDTO jwtResult = _jwtAuthManagerService.Refresh(refreshTokenRequestDTO, accesTokenUserId, emailFromTheDb);

            return new LoginResultDTO
            {
                UserId = (long)jwtResult.UserId, // Here it will always be user, if there is not, it will break earlier
                Email = jwtResult.UserEmail,
                AccessToken = jwtResult.AccessToken,
                RefreshToken = jwtResult.Token.TokenString
            };
        }

        public async Task<string> GetCurrentUserEmailByIdAsync(long id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<TUser>().AsNoTracking().Where(x => x.Id == id).Select(x => x.Email).SingleOrDefaultAsync();
            });
        }

        public async Task<TUser> GetUserByEmailAsync(string email)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<TUser>().AsNoTracking().Where(x => x.Email == email).SingleOrDefaultAsync();
            });
        }

        #endregion

        #region Helpers

        private JwtAuthResultDTO GetJwtAuthResultWithRefreshDTO(long userId, string userEmail, string browserId)
        {
            string ipAddress = _authenticationService.GetIPAddress();

            JwtAuthResultDTO jwtAuthResult = _jwtAuthManagerService.GenerateAccessAndRefreshTokens(userId, userEmail, ipAddress, browserId);

            return jwtAuthResult;
        }

        private LoginResultDTO GetLoginResultDTO(long userId, string userEmail, JwtAuthResultDTO jwtAuthResultDTO)
        {
            return new LoginResultDTO
            {
                UserId = userId,
                Email = userEmail,
                AccessToken = jwtAuthResultDTO.AccessToken,
                RefreshToken = jwtAuthResultDTO.Token.TokenString,
            };
        }

        private async Task<TUser> Authenticate(LoginDTO loginDTO)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                TUser currentUser = await _context.DbSet<TUser>()
                    .Where(x => x.Email == loginDTO.Email)
                    .SingleOrDefaultAsync();

                if (currentUser == null)
                    throw new BusinessException(SharedTerms.AuthenticationEmailDoesNotExistException); // TODO FT: Resources

                if (currentUser.NumberOfFailedAttemptsInARow > SettingsProvider.Current.NumberOfFailedLoginAttemptsInARowToDisableUser)
                    throw new BusinessException(SharedTerms.DisabledAccountException); // TODO FT: We will let this for now, because now there is no other way to make the user can't login

                if (currentUser.Password == null)
                    throw new BusinessException(SharedTerms.OnlyThirdPartyAccountButTriedToLoginException);

                if (BCrypt.Net.BCrypt.EnhancedVerify(loginDTO.Password, currentUser.Password) == false)
                {
                    // TODO FT: Maybe this has not sence if we have 2fa with email and forgot password option
                    //currentUser.NumberOfFailedAttemptsInARow++;
                    //await _context.SaveChangesAsync();
                    throw new BusinessException(SharedTerms.AuthenticationIncorectPasswordException);
                }

                return currentUser;
            });
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string idToken, string clientId)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { clientId }
            };

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings); // TODO FT: Try to pass the wrong token
            return payload;
        }

        #endregion

        #region User

        public async Task<List<NamebookDTO<int>>> LoadRoleNamebookListForUserExtended(long userId)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                //await _authorizationService.AuthorizeAndThrowAsync<TUser>(Enums.PermissionCodes.ReadRole);

                return await _context.DbSet<TUser>()
                    .AsNoTracking()
                    .Where(x => x.Id == userId)
                    .SelectMany(x => x.Roles)
                    .OfType<Role>()
                    .Select(role => new NamebookDTO<int>
                    {
                        Id = role.Id,
                        DisplayName = role.Name,
                    })
                    .ToListAsync();
            });
        }

        public async Task UpdateRoleListForUser(long userId, List<int> selectedRoleIds)
        {
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await LoadInstanceAsync<TUser, long>(userId, null);

                foreach (Role role in user.Roles.ToList())
                {
                    if (selectedRoleIds.Contains(role.Id))
                        selectedRoleIds.Remove(role.Id);
                    else
                        user.Roles.Remove(role);
                }

                List<Role> roleListToInsert = await _context.DbSet<Role>().Where(x => selectedRoleIds.Contains(x.Id)).ToListAsync();

                user.Roles.AddRange(roleListToInsert);
                await _context.SaveChangesAsync();
            });
        }

        #endregion

        #region Role

        public async Task UpdateUserListForRole(int roleId, List<long> selectedUserIds)
        {
            if (selectedUserIds == null)
                return;

            await _context.WithTransactionAsync(async () =>
            {
                List<RoleUser> roleUserList = await _context.DbSet<RoleUser>().Where(x => x.RolesId == roleId).ToListAsync();

                foreach (RoleUser roleUser in roleUserList)
                {
                    if (selectedUserIds.Contains(roleUser.UsersId))
                        selectedUserIds.Remove(roleUser.UsersId);
                    else
                        _context.DbSet<RoleUser>().Remove(roleUser);
                }

                foreach (long selectedUserId in selectedUserIds)
                {
                    RoleUser roleUser = new RoleUser 
                    {
                        RolesId = roleId,
                        UsersId = selectedUserId
                    };

                    await _context.DbSet<RoleUser>().AddAsync(roleUser);
                }
                

                await _context.SaveChangesAsync();
            });
        }

        public async Task<List<NamebookDTO<long>>> LoadUserExtendedNamebookListForRole(long roleId)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                await _authorizationService.AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadUserExtended);

                return await _context.DbSet<TUser>()
                    .AsNoTracking()
                    .Where(x => x.Roles.Any(x => x.Id == roleId))
                    .Select(x => new NamebookDTO<long>
                    {
                        Id = x.Id,
                        DisplayName = x.Email,
                    })
                    .ToListAsync();
            });
        }

        public async Task<RoleDTO> SaveRoleAndReturnDTOExtendedAsync(RoleSaveBodyDTO roleSaveBodyDTO)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                RoleDTO savedRoleDTO = await SaveRoleAndReturnDTOAsync(roleSaveBodyDTO.RoleDTO);

                await UpdateUserListForRole(savedRoleDTO.Id, roleSaveBodyDTO.SelectedUserIds);
                await UpdatePermissionListForRole(savedRoleDTO.Id, roleSaveBodyDTO.SelectedPermissionIds);

                return savedRoleDTO;
            });
        }

        #endregion

        //#region Notification

        //public async Task UpdateUserListForNotification<TUser>(long notificationId, bool isMarkAsRead, List<long> selectedUserIds) where TUser : class, IUser, new()
        //{
        //    if (selectedUserIds == null)
        //        return;

        //    await _context.WithTransactionAsync(async () =>
        //    {
        //        List<NotificationUser> notificationUserList = await _context.DbSet<NotificationUser>().Where(x => x.NotificationsId == notificationId).ToListAsync();

        //        foreach (NotificationUser notificationUser in notificationUserList)
        //        {
        //            if (selectedUserIds.Contains(notificationUser.UsersId))
        //                selectedUserIds.Remove(notificationUser.UsersId);
        //            else
        //                _context.DbSet<NotificationUser>().Remove(notificationUser);
        //        }

        //        foreach (long selectedUserId in selectedUserIds)
        //        {
        //            NotificationUser notificationUser = new NotificationUser
        //            {
        //                NotificationsId = notificationId,
        //                UsersId = selectedUserId,
        //                IsMarkedAsRead = isMarkAsRead,
        //            };

        //            await _context.DbSet<NotificationUser>().AddAsync(notificationUser);
        //        }


        //        await _context.SaveChangesAsync();
        //    });
        //}

        //public async Task<List<NamebookDTO<long>>> LoadUserExtendedNamebookListForNotification<TUser>(long notificationId) where TUser : class, IUser, new()
        //{
        //    return await _context.WithTransactionAsync(async () =>
        //    {
        //        await _authorizationService.AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadUserExtended);

        //        return await _context.DbSet<TUser>()
        //            .AsNoTracking()
        //            .Where(x => x.Notifications.Any(x => x.Id == notificationId))
        //            .Select(x => new NamebookDTO<long>
        //            {
        //                Id = x.Id,
        //                DisplayName = x.Email,
        //            })
        //            .ToListAsync();
        //    });
        //}

        //public async Task<TableResponseDTO<NotificationDTO>> LoadNotificationListForTheCurrentUser<TUser>(TableFilterDTO tableFilterDTO) where TUser : class, IUser, new()
        //{
        //    TableResponseDTO<NotificationDTO> result = new TableResponseDTO<NotificationDTO>();
        //    long currentUserId = _authenticationService.GetCurrentUserId(); // FT: Not doing user.Notifications, because he could have a lot of them.

        //    await _context.WithTransactionAsync(async () =>
        //    {
        //        int count = await _context.DbSet<NotificationUser>().Where(x => x.UsersId == currentUserId).CountAsync();

        //        List<NotificationDTO> notificationsDTO = await _context.DbSet<TUser>()
        //            .Where(x => x.Id == currentUserId)
        //            .SelectMany(x => x.Notifications)
        //            .OrderByDescending(x => x.CreatedAt)
        //            .Skip(tableFilterDTO.First)
        //            .Take(tableFilterDTO.Rows)
        //            .Select(x => new NotificationDTO
        //            {
        //                Id = x.Id,
        //                Title = x.Title,
        //                Description = x.Description,
        //                IsMarkedAsRead = _context.DbSet<NotificationUser>().Where(i => i.NotificationsId == x.Id).Select(x => x.IsMarkedAsRead).SingleOrDefault()
        //            })
        //            .ToListAsync();
        //        result.Data = notificationsDTO;
        //        result.TotalRecords = count;
        //    });

        //    return result;
        //}

        //public async Task<int> GetUnreadNotificationCountForTheCurrentUser()
        //{
        //    long currentUserId = _authenticationService.GetCurrentUserId();

        //    return await _context.WithTransactionAsync(async () =>
        //    {
        //        return await _context.DbSet<NotificationUser>().Where(x => x.UsersId == currentUserId && x.IsMarkedAsRead == false).CountAsync();
        //    });
        //}

        //#endregion

    }
}
