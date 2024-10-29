using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.SoftExceptions;
using Soft.Generator.Shared.Terms;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Soft.Generator.Security.Services
{
    public class JwtAuthManagerService : IJwtAuthManager
    {
        public IImmutableDictionary<string, RefreshTokenDTO> UsersRefreshTokensReadOnlyDictionary => _usersRefreshTokens.ToImmutableDictionary();
        // Making ConcurrentDictionary if two users are searching for the refresh token in the same time, use Redis in the future
        // The maximum number of the refresh tokens inside dictionary is SettingsProvider.Current.AllowedBrowsersForTheSingleUser
        private readonly ConcurrentDictionary<string, RefreshTokenDTO> _usersRefreshTokens = new ConcurrentDictionary<string, RefreshTokenDTO>();

        public IImmutableDictionary<string, RegistrationVerificationTokenDTO> UsersRegistrationVerificationTokensReadOnlyDictionary => _usersRegistrationVerificationTokens.ToImmutableDictionary();
        private readonly ConcurrentDictionary<string, RegistrationVerificationTokenDTO> _usersRegistrationVerificationTokens = new ConcurrentDictionary<string, RegistrationVerificationTokenDTO>();

        public IImmutableDictionary<string, LoginVerificationTokenDTO> UsersLoginVerificationTokensReadOnlyDictionary => _usersLoginVerificationTokens.ToImmutableDictionary();
        private readonly ConcurrentDictionary<string, LoginVerificationTokenDTO> _usersLoginVerificationTokens = new ConcurrentDictionary<string, LoginVerificationTokenDTO>();

        public IImmutableDictionary<string, ForgotPasswordVerificationTokenDTO> UsersForgotPasswordVerificationTokensReadOnlyDictionary => _usersForgotPasswordVerificationTokens.ToImmutableDictionary();
        private readonly ConcurrentDictionary<string, ForgotPasswordVerificationTokenDTO> _usersForgotPasswordVerificationTokens = new ConcurrentDictionary<string, ForgotPasswordVerificationTokenDTO>();

        private static readonly Random Random = new Random();

        public JwtAuthManagerService()
        {
        }

        #region Refresh

        /// <summary>
        /// 1. Stole refresh but doesn't have access - we validate if he has access
        /// 2. Stole a refresh from one user, he has his own valid access - we log both of them out because they have different emails
        /// 3. Stole access but no refresh
        /// 4. Stole both - we can't do anything to him, we only try to stop him if he's on a different ip address
        /// </summary>
        public JwtAuthResultDTO Refresh(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims)
        {
            RemoveExpiredRefreshTokens();
            // FT: We can assume that dbUserEmail and refreshTokenEmail are the same, because if they are not anyway we will go through and delete everything
            RemoveTokensForMoreThenAllowedBrowsers(dbUserEmail);

            // FT: Sometimes in development mode, when the multiple tabs are open, on the save of the angular app we refresh the tabs in the same time, so we don't even manage to change the value of the refresh token in the local storage,
            // and we send another request with the same refresh token as the previous one, and since we deleted it, it doesn't exist
            if (!_usersRefreshTokens.TryGetValue(request.RefreshToken, out RefreshTokenDTO existingRefreshToken))
            {
                throw new SecurityTokenException(SharedTerms.ExpiredRefreshTokenException); // The token has expired
            }
            // Unauthenticating both user, this could happen if someone stoled access token (aleksa.trivan), and has own valid refresh token (filip.trivan), he could indefinedly generate access tokens for the (aleksa.trivan) then
            // This is not solving this problem (hacker can not change claims in the jwt token): https://stackoverflow.com/questions/27301557/if-you-can-decode-jwt-how-are-they-secure we are doing that with Decoding JWT token.
            // It is not posible for the user to change the email of the refresh token, even if it is, if the user change the email in the refresh token, it doesn't matter, we will find based on the refresh token code not email
            if (existingRefreshToken.Email != dbUserEmail) // FT: Could happen if someone gives me access and refresh for different users, i don't know which of these he stole so i unauthenticate both
            {
                RemoveRefreshTokenByEmail(existingRefreshToken.Email);
                RemoveRefreshTokenByEmail(dbUserEmail);
                throw new HackerException("The email can't be different in refresh and access token.");
            }
            if (SettingsProvider.Current.AllowTheUseOfAppWithDifferentIpAddresses == false && IsRefreshTokenWithNewIpAddress(existingRefreshToken.Email, existingRefreshToken.IpAddress) == true)
            {
                // cuvas device-ove koje je cesto korisio, guras ih u familiju uredjaja, po nekom algoritmu odredi neki koji ti se cini sumnjiv i
                // na njemu mu trazi multifaktor aut. ako je klijent uopste trazio multifaktor
                RemoveRefreshTokenByEmail(existingRefreshToken.Email); // Don't need to delete for userDTO also, because we already did that
                throw new SecurityTokenException(SharedTerms.TwoDifferentIpAddressesRefreshException);
            }

            return GenerateAccessAndRefreshTokens(dbUserEmail, principalClaims, existingRefreshToken.IpAddress, request.BrowserId, dbUserId); // need to recover the original claims
        }

        // FT: REFRESH HACK
        public JwtAuthResultDTO RefreshDevHack(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims)
        {
            RefreshTokenDTO refreshTokenDTO = new RefreshTokenDTO
            {
                BrowserId = request.BrowserId,
                Email = "filiptrivan5@gmail.com",
                ExpireAt = DateTime.Now.AddDays(30),
                IpAddress = "1",
                TokenString = request.RefreshToken,
            };

            _usersRefreshTokens.AddOrUpdate("1", refreshTokenDTO, (_, _) => refreshTokenDTO);

            if (!_usersRefreshTokens.TryGetValue(request.RefreshToken, out RefreshTokenDTO existingRefreshToken))
            {
                throw new SecurityTokenException("Invalid token, login again."); // The token has expired
            }

            return GenerateAccessAndRefreshTokens(dbUserEmail, principalClaims, existingRefreshToken.IpAddress, request.BrowserId, dbUserId); // need to recover the original claims
        }

        /// <summary>
        /// Password and verificationExpiration (minutes) are only needed if we are registering the account, for email verification
        /// </summary>
        public JwtAuthResultDTO GenerateAccessAndRefreshTokens(string userEmail, List<Claim> claims, string ipAddress, string browserId, long userId)
        {
            string accessToken = GenerateAccessToken(claims);
            RefreshTokenDTO refreshTokenDTO = new RefreshTokenDTO
            {
                Email = userEmail,
                IpAddress = ipAddress,
                BrowserId = browserId,
                TokenString = GenerateRandomTokenString(),
                ExpireAt = DateTime.Now.AddMinutes(SettingsProvider.Current.RefreshTokenExpiration),
            };
            RemoveTheLastRefreshTokenFromTheSameBrowserAndEmail(browserId, userEmail); // And email also, because the one man can be logged in on the same browser as multiple users
            // It will always generate new token,
            // it is beneficial if the user open the application from different devices
            // if the user open the application on the multiple tabs in the same browser, we are working with the local storage so it will not make the difference
            _usersRefreshTokens.AddOrUpdate(refreshTokenDTO.TokenString, refreshTokenDTO, (_, _) => refreshTokenDTO);
            return new JwtAuthResultDTO
            {
                UserId = userId,
                UserEmail = userEmail,
                AccessToken = accessToken,
                Token = refreshTokenDTO
            };
        }

        #region Helpers

        private string GenerateAccessToken(List<Claim> claims, int? verificationExpiration = null)
        {
            byte[] secretKey = Encoding.UTF8.GetBytes(SettingsProvider.Current.JwtKey);
            SigningCredentials credentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

            bool shouldAddAudienceClaim = string.IsNullOrWhiteSpace(claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Aud)?.Value);
            JwtSecurityToken jwtToken = new JwtSecurityToken(
                SettingsProvider.Current.JwtIssuer,
                shouldAddAudienceClaim ? SettingsProvider.Current.JwtAudience : string.Empty,
                claims,
                expires: DateTime.Now.AddMinutes(verificationExpiration ?? SettingsProvider.Current.AccessTokenExpiration),
                signingCredentials: credentials);

            string accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return accessToken;
        }

        public List<Claim> GetPrincipalClaimsForAccessToken(RefreshTokenRequestDTO request, string accessToken)
        {
            List<Claim> principalClaims;

            try
            {
                var (principal, jwtToken) = DecodeJwtToken(accessToken);
                principalClaims = principal.Claims.ToList();
            }
            catch (Exception)
            {
                _usersRefreshTokens.TryRemove(request.RefreshToken, out _); // FT: If the user hadn't access token but trying somehow to do something ilegal, remove the passed refresh also
                throw;
            }

            return principalClaims; // FT: Its not possible to return null, if there is no exception it will return, if there is the catch block will throw
        }

        /// <summary>
        /// FT: I don't know do i even need to validate this old access token for Refresh
        /// I think it's okay to do this because i need claims, if i don't get claims here i would need to load him from the database because the id of the user isnt in the refresh token
        /// </summary>
        public (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new SecurityTokenException("Invalid token.");

            byte[] secretKey = Encoding.UTF8.GetBytes(SettingsProvider.Current.JwtKey);

            ClaimsPrincipal principal = new JwtSecurityTokenHandler()
                .ValidateToken(accessToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = SettingsProvider.Current.JwtIssuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                        ValidAudience = SettingsProvider.Current.JwtAudience,
                        ValidateAudience = true, // Checking if the audience is the valid one (localhost:7260)
                        ValidateLifetime = false, // If the token has expired, it will not be valid, so we don't need to do something like this: if (existingRefreshToken.ExpireAt - jwtToken.ExpireAt > SettingsProvider.Current.RefreshTokenExpiration - SettingsProvider.Current.AccessTokenExpiration) ...
                        ClockSkew = TimeSpan.FromMinutes(SettingsProvider.Current.ClockSkewMinutes)
                    },
            out var validatedToken);

            JwtSecurityToken jwtToken = validatedToken as JwtSecurityToken;

            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature)) // Validating JWT token, checking if it has changed claims etc.
                throw new SecurityTokenException("Invalid token.");

            return (principal, jwtToken);
        }

        public void RemoveTheLastRefreshTokenFromTheSameBrowserAndEmail(string browserId, string email)
        {
            // TODO FT: Log if the email is null

            KeyValuePair<string, RefreshTokenDTO> refreshToken = _usersRefreshTokens.Where(x => x.Value.BrowserId == browserId && x.Value.Email == email).SingleOrDefault(); // FT: REFRESH HACK
            if (!string.IsNullOrEmpty(refreshToken.Key))
                _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
            else
                RemoveRefreshTokenByEmail(email); // FT: if someone deleted the browser id, he could't log out if we don't do this.
        }

        public void RemoveExpiredRefreshTokens()
        {
            var expiredTokens = _usersRefreshTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
                _usersRefreshTokens.TryRemove(expiredToken.Key, out _);
        }

        public void RemoveRefreshTokenByEmail(string email)
        {
            var refreshTokens = _usersRefreshTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var refreshToken in refreshTokens)
                _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
        }

        private static string GenerateRandomTokenString()
        {
            byte[] randomNumber = new byte[32]; // It would take approximately 1.84 x 10^60 years to guess the token using brute force at a rate of 1 billion guesses per second which is also nearly imposible
            using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Base64UrlEncoder.Encode(randomNumber); // FT: Making it url safe
        }

        private bool IsRefreshTokenWithNewIpAddress(string email, string ipAddress)
        {
            if (_usersRefreshTokens.Where(x => x.Value.Email == email).OrderByDescending(x => x.Value.ExpireAt).FirstOrDefault().Value?.IpAddress != ipAddress)
                return true;
            else
                return false;
        }

        private void RemoveTokensForMoreThenAllowedBrowsers(string email)
        {
            List<KeyValuePair<string, RefreshTokenDTO>> refreshTokens = _usersRefreshTokens.Where(x => x.Value.Email == email).ToList();
            if (refreshTokens.Count > SettingsProvider.Current.AllowedBrowsersForTheSingleUser)
            {
                List<KeyValuePair<string, RefreshTokenDTO>> excessBrowserRefreshTokens = refreshTokens.OrderBy(x => x.Value.ExpireAt).Take(refreshTokens.Count - SettingsProvider.Current.AllowedBrowsersForTheSingleUser).ToList();
                foreach (KeyValuePair<string, RefreshTokenDTO> refreshToken in excessBrowserRefreshTokens)
                {
                    _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
                }
            }
        }

        #endregion

        #endregion


        #region Verification

        #region Login

        public LoginVerificationTokenDTO ValidateAndGetLoginVerificationTokenDTO(string verificationTokenKey, string browserId, string email)
        {
            RemoveExpiredLoginVerificationTokens();

            // FT: Doing this because there is a chance of generating two same codes.
            LoginVerificationTokenDTO loginVerificationTokenDTO = _usersLoginVerificationTokens.Where(x => x.Key == verificationTokenKey && x.Value.Email == email && x.Value.BrowserId == browserId).SingleOrDefault().Value;

            if (loginVerificationTokenDTO == null)
                throw new ExpiredVerificationException(SharedTerms.ExpiredLoginVerificationCodeException); // FT: We can not allow user to "send again" from here, because it is deleted

            KeyValuePair<string, LoginVerificationTokenDTO> lastVerificationToken = _usersLoginVerificationTokens
                .Where(x => x.Value.Email == loginVerificationTokenDTO.Email)
                .OrderByDescending(x => x.Value.ExpireAt)
                .FirstOrDefault();

            if (verificationTokenKey != lastVerificationToken.Key)
                throw new ExpiredVerificationException(SharedTerms.LatestVerificationCodeException);

            return loginVerificationTokenDTO;
        }

        public string GenerateAndSaveLoginVerificationCode(string userEmail, long userId, string browserId)
        {
            LoginVerificationTokenDTO loginVerificationTokenDTO = new LoginVerificationTokenDTO
            {
                Email = userEmail,
                UserId = userId,
                BrowserId = browserId,
                ExpireAt = DateTime.Now.AddMinutes(SettingsProvider.Current.VerificationTokenExpiration),
            };

            string code = GenerateVerificationCodeKey();
            _usersLoginVerificationTokens.AddOrUpdate(code, loginVerificationTokenDTO, (_, _) => loginVerificationTokenDTO);
            return code;
        }

        #endregion

        #region Forgot password

        public ForgotPasswordVerificationTokenDTO ValidateAndGetForgotPasswordVerificationTokenDTO(string verificationTokenKey, string browserId, string email)
        {
            RemoveExpiredForgotPasswordVerificationTokens();

            // FT: Doing this because there is a chance of generating two same codes.
            ForgotPasswordVerificationTokenDTO forgotPasswordVerificationTokenDTO = _usersForgotPasswordVerificationTokens.Where(x => x.Key == verificationTokenKey && x.Value.Email == email && x.Value.BrowserId == browserId).SingleOrDefault().Value;

            if (forgotPasswordVerificationTokenDTO == null)
            {
                throw new ExpiredVerificationException("The verification code has expired."); // We can not give allow user to send again from here, because it is deleted
            }
            KeyValuePair<string, ForgotPasswordVerificationTokenDTO> lastVerificationToken = _usersForgotPasswordVerificationTokens
                .Where(x => x.Value.Email == forgotPasswordVerificationTokenDTO.Email)
                .OrderByDescending(x => x.Value.ExpireAt)
                .FirstOrDefault();
            if (verificationTokenKey != lastVerificationToken.Key)
            {
                throw new ExpiredVerificationException("Please, use the latest code sent.");
            }

            return forgotPasswordVerificationTokenDTO;
        }

        /// <summary>
        /// userId because the user exists, when verify registration, user doesn't exist and we are making the userId
        /// </summary>
        public string GenerateAndSaveForgotPasswordVerificationCode(string userEmail, long userId, string newPassword, string browserId)
        {
            ForgotPasswordVerificationTokenDTO forgotPasswordVerificationTokenDTO = new ForgotPasswordVerificationTokenDTO
            {
                Email = userEmail,
                UserId = userId,
                NewPassword = newPassword,
                BrowserId = browserId,
                ExpireAt = DateTime.Now.AddMinutes(SettingsProvider.Current.VerificationTokenExpiration),
            };

            string code = GenerateVerificationCodeKey();
            _usersForgotPasswordVerificationTokens.AddOrUpdate(code, forgotPasswordVerificationTokenDTO, (_, _) => forgotPasswordVerificationTokenDTO);
            return code;
        }

        #endregion

        #region Registration

        public RegistrationVerificationTokenDTO ValidateAndGetRegistrationVerificationTokenDTO(string verificationTokenKey, string browserId, string email)
        {
            RemoveExpiredRegistrationVerificationTokens();

            // FT: Doing this because there is a chance of generating two same codes.
            RegistrationVerificationTokenDTO registrationVerificationTokenDTO = _usersRegistrationVerificationTokens
                .Where(x => x.Key == verificationTokenKey && x.Value.Email == email && x.Value.BrowserId == browserId).SingleOrDefault().Value;

            if (registrationVerificationTokenDTO == null)
            {
                throw new ExpiredVerificationException("The verification code has expired."); // We can not give allow user to send again from here, because it is deleted
            }
            KeyValuePair<string, RegistrationVerificationTokenDTO> lastVerificationToken = _usersRegistrationVerificationTokens
                .Where(x => x.Value.Email == registrationVerificationTokenDTO.Email)
                .OrderByDescending(x => x.Value.ExpireAt)
                .FirstOrDefault();
            if (verificationTokenKey != lastVerificationToken.Key)
            {
                throw new ExpiredVerificationException("Please, use the latest code sent.");
            }

            return registrationVerificationTokenDTO;
        }

        public string GenerateAndSaveRegistrationVerificationCode(string userEmail, string password, string browserId)
        {
            RegistrationVerificationTokenDTO registrationVerificationTokenDTO = new RegistrationVerificationTokenDTO
            {
                Email = userEmail,
                Password = password,
                BrowserId = browserId,
                ExpireAt = DateTime.Now.AddMinutes(SettingsProvider.Current.VerificationTokenExpiration),
            };

            string code = GenerateVerificationCodeKey();
            _usersRegistrationVerificationTokens.AddOrUpdate(code, registrationVerificationTokenDTO, (_, _) => registrationVerificationTokenDTO);
            return code;
        }

        #endregion

        #region Helpers

        private static string GenerateVerificationCodeKey()
        {
            int code = Random.Next(100000, 1000000);
            return code.ToString("D6");
        }

        public void RemoveLoginVerificationTokensByEmail(string email)
        {
            var verificationTokens = _usersLoginVerificationTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var verificationToken in verificationTokens)
            {
                _usersLoginVerificationTokens.TryRemove(verificationToken.Key, out _);
            }
        }

        private void RemoveExpiredLoginVerificationTokens()
        {
            var expiredTokens = _usersLoginVerificationTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
            {
                _usersLoginVerificationTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        public void RemoveForgotPasswordVerificationTokensByEmail(string email)
        {
            var verificationTokens = _usersForgotPasswordVerificationTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var verificationToken in verificationTokens)
            {
                _usersForgotPasswordVerificationTokens.TryRemove(verificationToken.Key, out _);
            }
        }

        private void RemoveExpiredForgotPasswordVerificationTokens()
        {
            var expiredTokens = _usersForgotPasswordVerificationTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
            {
                _usersForgotPasswordVerificationTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        public void RemoveRegistrationVerificationTokensByEmail(string email)
        {
            var verificationTokens = _usersRegistrationVerificationTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var verificationToken in verificationTokens)
            {
                _usersRegistrationVerificationTokens.TryRemove(verificationToken.Key, out _);
            }
        }

        private void RemoveExpiredRegistrationVerificationTokens()
        {
            var expiredTokens = _usersRegistrationVerificationTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
            {
                _usersRegistrationVerificationTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        #endregion

        #endregion

    }
}
