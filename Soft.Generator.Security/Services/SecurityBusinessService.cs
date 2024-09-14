using Microsoft.EntityFrameworkCore;
using Soft.Generator.Security.DataMappers;
using Soft.Generator.Security.DTO;
using System.Security.Claims;
using Soft.Generator.Shared.Excel;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Security.Entities;
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

namespace Soft.Generator.Security.Services
{
    public class SecurityBusinessService : SecurityBusinessServiceGenerated
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly ExcelService _excelService;
        private readonly EmailingService _emailingService;

        public SecurityBusinessService(IApplicationDbContext context, ExcelService excelService, IJwtAuthManager jwtAuthManagerService, EmailingService emailingService)
        : base(context, excelService)
        {
            _context = context;
            _excelService = excelService;
            _jwtAuthManagerService = jwtAuthManagerService;
            _emailingService = emailingService;
        }

        public async Task<LoginResultDTO> Login(LoginDTO loginDTO, string ipAddress)
        {
            try
            {
                LoginDTOValidationRules validationRules = new LoginDTOValidationRules();
                validationRules.ValidateAndThrow(loginDTO);

                return await _context.WithTransactionAsync(async () =>
                {
                    User user = await Authenticate(loginDTO);
                    JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(ipAddress, loginDTO.BrowserId, user.Id, user.Email);
                    //await SaveLoginAndReturnDomainAsync(loginDTO); // FT: Is ipAddress == null is checked here // TODO FT: Log it
                    return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
                });
            }
            catch (Exception)
            {
                // We don't want to add NumberOfFailedAttemptsInARow to the user, if something went to the catch block, only if he typed bad password
                // TODO FT: log it
                //loginDTO.IsSuccessful = false;
                //await SaveLoginAndReturnDomainAsync(loginDTO);
                throw;
            }
        }

        public async Task<LoginResultDTO> LoginExternal(ExternalProviderDTO externalProviderDTO, string ipAddress, string googleClientId)
        {
            //LoginDTO loginDTO = new LoginDTO();
            //loginDTO.IpAddress = ipAddress;
            //loginDTO.IsExternal = true;
            //loginDTO.BrowserId = externalProviderDTO.BrowserId;
            try
            {
                GoogleJsonWebSignature.Payload payload = await ValidateGoogleToken(externalProviderDTO.IdToken, googleClientId);
                //loginDTO.Email = payload.Email;
                return await _context.WithTransactionAsync(async () =>
                {
                    User user = await GetUserByEmailAsync(payload.Email); // FT: Check if user already exist in the database
                    DbSet<User> userDbSet = _context.DbSet<User>();

                    if (user == null)
                    {
                        user = new User
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
                            throw new BusinessException("Your account is disabled, please contact the administrator.");

                        if (user.HasLoggedInWithExternalProvider == false)
                            await userDbSet.ExecuteUpdateAsync(x => x.SetProperty(x => x.HasLoggedInWithExternalProvider, true)); // There is no need for SaveChangesAsync because we don't need to update the version of the user
                    }

                    JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(ipAddress, externalProviderDTO.BrowserId, user.Id, user.Email);
                    // TODO FT: Log it
                    //loginDTO.IsSuccessful = true;
                    //await SaveLoginExternalAsync(loginDTO);
                    return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
                });
            }
            catch (Exception)
            {
                // TODO FT: Log it
                //loginDTO.IsSuccessful = false;
                //await SaveLoginExternalAsync(loginDTO);
                throw;
            }
        }

        public async Task<RegistrationResultDTO> Register(RegistrationDTO registrationDTO, string ipAddress)
        {
            RegistrationResultDTO registrationResultDTO = new RegistrationResultDTO();

            try
            {
                RegistrationDTOValidationRules validationRules = new RegistrationDTOValidationRules();
                validationRules.ValidateAndThrow(registrationDTO);

                await _context.WithTransactionAsync(async () =>
                {
                    User user = await GetUserByEmailAsync(registrationDTO.Email);
                    if (user == null)
                    {
                        JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRegistrationVerificationDTO(registrationDTO.Email, SettingsProvider.Current.VerificationTokenExpiration, registrationDTO.Password);
                        try
                        {
                            await _emailingService.SendVerificationEmailAsync(registrationDTO.Email, jwtAuthResultDTO.AccessToken, jwtAuthResultDTO.Token.TokenString);
                        }
                        catch (Exception)
                        {
                            _jwtAuthManagerService.RemoveVerificationTokensByEmail(registrationDTO.Email); // We didn't send email, set all verification tokens invalid then
                            throw;
                        }
                        registrationResultDTO.Status = RegistrationResultStatusCodes.UserDoesNotExistAndDoesNotHaveValidToken; // FT: We don't need to show the message to the user here, we will route him to another page
                    }
                    else if (user.HasLoggedInWithExternalProvider && user.Password == null)
                    {
                        registrationResultDTO.Status = RegistrationResultStatusCodes.UserWithoutPasswordExists;
                        registrationResultDTO.Message = "Your account already exists with third-party (eg. Google) authentication. If this is you and you want to set up an password as well, please log in to your profile and add a password.";
                    }
                    else if (user.Password != null)
                    {
                        registrationResultDTO.Status = RegistrationResultStatusCodes.UserWithPasswordExists;
                        registrationResultDTO.Message = "An account with this email address already exists in our system.";
                    }
                });
            }
            catch (Exception ex)
            {
                registrationResultDTO.Status = RegistrationResultStatusCodes.UnexpectedError;
                // TODO FT: log it
                throw;
            }

            return registrationResultDTO;
        }

        public async Task<LoginResultDTO> RegistrationVerification(string verificationToken, string accessToken, string ipAddress, string browserId)
        {
            _jwtAuthManagerService.DecodeJwtToken(accessToken);
            RefreshTokenDTO verificationTokenDTO = _jwtAuthManagerService.ValidateAndGetVerificationTokenDTO(verificationToken); // FT: Can not be null, if its null it already has thrown
            User user = null;
            await _context.WithTransactionAsync(async () =>
            {
                user = new User
                {
                    Email = verificationTokenDTO.Email,
                    Password = verificationTokenDTO.Password,
                    IsVerified = true,
                    HasLoggedInWithExternalProvider = false, // FT: He couldn't do this if already has account
                    NumberOfFailedAttemptsInARow = 0
                };
                await _context.DbSet<User>().AddAsync(user);
                await _context.SaveChangesAsync();
            });
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(ipAddress, browserId, user.Id, user.Email); // FT: User can't be null, it would throw earlier if he is
            //await SaveLoginAndReturnDomainAsync(loginDTO); // FT: Is ipAddress == null is checked here // TODO FT: Log it
            return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
        }

        /// <summary>
        /// userId is nullable because of calling method from registration
        /// </summary>
        public JwtAuthResultDTO GetJwtAuthResultWithRefreshDTO(string ipAddress, string browserId, long userId, string userEmail)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.PrimarySid, userId.ToString()),
                new Claim(ClaimTypes.Email, userEmail),
            };

            JwtAuthResultDTO jwtAuthResult = _jwtAuthManagerService.GenerateAccessAndRefreshTokens(userEmail, claims, ipAddress, browserId, userId);

            return jwtAuthResult;
        }

        public JwtAuthResultDTO GetJwtAuthResultWithRegistrationVerificationDTO(string userEmail, int verificationTokenExpiration, string password)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, userEmail),
            };

            JwtAuthResultDTO jwtAuthResult = _jwtAuthManagerService.GenerateAccessAndRegistrationVerificationTokens(userEmail, claims, verificationTokenExpiration, password);

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

        /// <summary>
        /// FT HACK: Because the DTO validation and db validations are different (i can save null in password for the external provider login user)
        /// </summary>
        [Obsolete("Old logic, where we stored all login attempts in the database, we chose to only log it.")]
        private async Task SaveLoginExternalAsync(LoginDTO loginDTO)
        {
            //if (loginDTO.IpAddress == null)
            //    throw new BusinessException("Your IP address is empty, contact support.");

            //await _context.WithTransactionAsync(async () =>
            //{
            //    DbSet<Login> dbSet = _context.DbSet<Login>();
            //    Login login = Mapper.Map(loginDTO);
            //    await dbSet.AddAsync(login);

            //    await _context.SaveChangesAsync();
            //});
        }

        private async Task<User> Authenticate(LoginDTO loginDTO)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                User currentUser = await _context.DbSet<User>()
                    .Where(x => x.Email == loginDTO.Email)
                    .SingleOrDefaultAsync();

                if (currentUser == null)
                    throw new BusinessException("You have entered a wrong email."); // TODO FT: Resources

                if (currentUser.NumberOfFailedAttemptsInARow > SettingsProvider.Current.NumberOfFailedLoginAttemptsInARowToDisableUser) // FT: It could never be 21 if the value from settings is 20, but putting > just in case
                    throw new BusinessException($"You have entered the wrong password {SettingsProvider.Current.NumberOfFailedLoginAttemptsInARowToDisableUser} times in a row, your account has been disabled.");

                if (BCrypt.Net.BCrypt.EnhancedVerify(loginDTO.Password, currentUser.Password) == false)
                {
                    currentUser.NumberOfFailedAttemptsInARow++;
                    await _context.SaveChangesAsync();
                    throw new BusinessException("You have entered a wrong password.");
                }

                return currentUser;
            });
        }

        public async Task<LoginResultDTO> GetLoginResultDTOAsync(RefreshTokenRequestDTO request, string accessToken)
        {
            List<Claim> principalClaims = _jwtAuthManagerService.GetPrincipalClaimsForAccessToken(request, accessToken);
            UserDTO userIdAndEmailFromTheAccessToken = GetCurrentUserIdAndEmailFromClaims(principalClaims); // FT: Id will be always the same, it can't change
            string emailFromTheDb = await GetCurrentUserEmailByIdAsync(userIdAndEmailFromTheAccessToken.Id);
            if (emailFromTheDb != userIdAndEmailFromTheAccessToken.Email) // The email from db changed, and the user is using the old one in access token
                _jwtAuthManagerService.RemoveRefreshTokenByEmail(userIdAndEmailFromTheAccessToken.Email);

            JwtAuthResultDTO jwtResult = _jwtAuthManagerService.Refresh(request, userIdAndEmailFromTheAccessToken.Id, emailFromTheDb, principalClaims);

            return new LoginResultDTO
            {
                UserId = (long)jwtResult.UserId, // Here it will always be user, if there is not, it will break earlier
                Email = jwtResult.UserEmail,
                AccessToken = jwtResult.AccessToken,
                RefreshToken = jwtResult.Token.TokenString
            };
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

        public static UserDTO GetCurrentUserIdAndEmail(ClaimsIdentity identity)
        {
            if (identity != null)
            {
                List<Claim> userClaims = identity.Claims.ToList();

                return GetCurrentUserIdAndEmailFromClaims(userClaims);
            }

            return null;
        }

        public static UserDTO GetCurrentUserIdAndEmailFromClaims(List<Claim> claims)
        {
            return new UserDTO
            {
                Id = long.Parse(claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value),
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
            };
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<User>().AsNoTracking().Where(x => x.Email == email).SingleOrDefaultAsync();
            });
        }

        protected override void OnBeforeUserIsMapped(UserDTO dto)
        {
            dto.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password);
        }

        public async Task<string> GetCurrentUserEmailByIdAsync(long id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<User>().AsNoTracking().Where(x => x.Id == id).Select(x => x.Email).SingleOrDefaultAsync();
            });
        }

    }
}
