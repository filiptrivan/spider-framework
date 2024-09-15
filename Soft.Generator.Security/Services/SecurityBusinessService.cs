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

        // Login
        public async Task SendLoginVerificationEmail(LoginDTO loginDTO)
        {
            LoginDTOValidationRules validationRules = new LoginDTOValidationRules();
            validationRules.ValidateAndThrow(loginDTO);

            string userEmail = null;
            await _context.WithTransactionAsync(async () =>
            {
                User user = await Authenticate(loginDTO);
                userEmail = user.Email;
            });
            string verificationCode = _jwtAuthManagerService.GenerateAndSaveLoginVerificationCode(userEmail);
            try
            {
                await _emailingService.SendVerificationEmailAsync(userEmail, verificationCode);
            }
            catch (Exception)
            {
                _jwtAuthManagerService.RemoveRegistrationVerificationTokensByEmail(userEmail); // We didn't send email, set all verification tokens invalid then
                throw;
            }
        }

        public async Task<LoginResultDTO> Login(VerificationTokenRequestDTO verificationRequestDTO, string ipAddress)
        {
            
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(ipAddress, loginDTO.BrowserId, user.Id, user.Email);
            return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
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


        // Registration
        public async Task<RegistrationVerificationResultDTO> SendRegistrationVerificationEmail(RegistrationDTO registrationDTO)
        {
            RegistrationVerificationResultDTO registrationResultDTO = new RegistrationVerificationResultDTO();

            try
            {
                RegistrationDTOValidationRules validationRules = new RegistrationDTOValidationRules();
                validationRules.ValidateAndThrow(registrationDTO);

                await _context.WithTransactionAsync(async () =>
                {
                    User user = await GetUserByEmailAsync(registrationDTO.Email);
                    if (user == null)
                    {
                        string verificationCode = _jwtAuthManagerService.GenerateAndSaveRegistrationVerificationCode(registrationDTO.Email, registrationDTO.Password);
                        try
                        {
                            await _emailingService.SendVerificationEmailAsync(registrationDTO.Email, verificationCode);
                        }
                        catch (Exception)
                        {
                            _jwtAuthManagerService.RemoveRegistrationVerificationTokensByEmail(registrationDTO.Email); // We didn't send email, set all verification tokens invalid then
                            throw;
                        }
                        registrationResultDTO.Status = RegistrationVerificationResultStatusCodes.UserDoesNotExistAndDoesNotHaveValidToken; // FT: We don't need to show the message to the user here, we will route him to another page
                    }
                    else if (user.HasLoggedInWithExternalProvider && user.Password == null)
                    {
                        registrationResultDTO.Status = RegistrationVerificationResultStatusCodes.UserWithoutPasswordExists;
                        registrationResultDTO.Message = "Your account already exists with third-party (eg. Google) authentication. If you want to set up an password as well, please log in to your profile and add a password.";
                    }
                    else if (user.Password != null)
                    {
                        registrationResultDTO.Status = RegistrationVerificationResultStatusCodes.UserWithPasswordExists;
                        registrationResultDTO.Message = "An account with this email address already exists in the system.";
                    }
                });
            }
            catch (Exception ex)
            {
                registrationResultDTO.Status = RegistrationVerificationResultStatusCodes.UnexpectedError;
                // TODO FT: log it
                throw;
            }

            return registrationResultDTO;
        }

        public async Task<LoginResultDTO> Register(VerificationTokenRequestDTO verificationRequestDTO, string ipAddress)
        {
            VerificationTokenRequestDTOValidationRules validationRules = new VerificationTokenRequestDTOValidationRules();
            validationRules.ValidateAndThrow(verificationRequestDTO);

            RegistrationVerificationTokenDTO verificationTokenDTO = _jwtAuthManagerService.ValidateAndGetRegistrationVerificationTokenDTO(verificationRequestDTO.VerificationCode, verificationRequestDTO.Email); // FT: Can not be null, if its null it already has thrown
            User user = null;
            await _context.WithTransactionAsync(async () =>
            {
                user = new User
                {
                    Email = verificationTokenDTO.Email,
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword(verificationTokenDTO.Password),
                    IsVerified = true,
                    HasLoggedInWithExternalProvider = false, // FT: He couldn't do this if already has account
                    NumberOfFailedAttemptsInARow = 0
                };
                await _context.DbSet<User>().AddAsync(user);
                await _context.SaveChangesAsync();
            });
            JwtAuthResultDTO jwtAuthResultDTO = GetJwtAuthResultWithRefreshDTO(ipAddress, verificationRequestDTO.BrowserId, user.Id, user.Email); // FT: User can't be null, it would throw earlier if he is
            //await SaveLoginAndReturnDomainAsync(loginDTO); // FT: Is ipAddress == null is checked here // TODO FT: Log it
            return GetLoginResultDTO(user.Id, user.Email, jwtAuthResultDTO);
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



        public static UserDTO GetCurrentUserIdAndEmail(ClaimsIdentity identity)
        {
            if (identity != null)
            {
                List<Claim> userClaims = identity.Claims.ToList();

                return GetCurrentUserIdAndEmailFromClaims(userClaims);
            }

            return null;
        }

        public async Task<string> GetCurrentUserEmailByIdAsync(long id)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<User>().AsNoTracking().Where(x => x.Id == id).Select(x => x.Email).SingleOrDefaultAsync();
            });
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await _context.DbSet<User>().AsNoTracking().Where(x => x.Email == email).SingleOrDefaultAsync();
            });
        }

        #region Helpers

        private JwtAuthResultDTO GetJwtAuthResultWithRefreshDTO(string ipAddress, string browserId, long userId, string userEmail)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.PrimarySid, userId.ToString()),
                new Claim(ClaimTypes.Email, userEmail),
            };

            JwtAuthResultDTO jwtAuthResult = _jwtAuthManagerService.GenerateAccessAndRefreshTokens(userEmail, claims, ipAddress, browserId, userId);

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

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string idToken, string clientId)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { clientId }
            };

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings); // TODO FT: Try to pass the wrong token
            return payload;
        }

        private static UserDTO GetCurrentUserIdAndEmailFromClaims(List<Claim> claims)
        {
            return new UserDTO
            {
                Id = long.Parse(claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value),
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
            };
        }

        protected override void OnBeforeUserIsMapped(UserDTO dto)
        {
            dto.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password);
        }

        #endregion

    }
}
