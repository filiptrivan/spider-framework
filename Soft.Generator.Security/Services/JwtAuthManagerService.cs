using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Entities;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.SoftExceptions;
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
        public IImmutableDictionary<string, RefreshTokenDTO> UsersVerificationTokensReadOnlyDictionary => _usersVerificationTokens.ToImmutableDictionary();
        private readonly ConcurrentDictionary<string, RefreshTokenDTO> _usersVerificationTokens = new ConcurrentDictionary<string, RefreshTokenDTO>();


        public JwtAuthManagerService()
        {
        }

        /// <summary>
        /// 1. Stole refresh but doesn't have access - we validate if he has access ()
        /// 2. Stole a refresh from one user, he has his own valid access - we log both of them out because they have different emails
        /// 3. Stole access but no refresh
        /// 4. Stole both - we can't do anything to him, we only try to stop him if he's on a different ip address
        /// </summary>
        public JwtAuthResultDTO Refresh(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims)
        {
            RemoveExpiredRefreshTokens();
            // FT: We can assume that dbUserEmail and refreshTokenEmail are the same, because if they are not anyway we will go through and delete everything
            RemoveTokensForMoreThenAllowedBrowsers(dbUserEmail);
            //RemoveVerificationTokenByEmail(dbUserEmail); // Always one in the dictionary // TODO FT: dont overhead

            // FT: Sometimes in development mode, when the multiple tabs are open, on the save of the angular app we refresh the tabs in the same time, so we don't even manage to change the value of the refresh token in the local storage,
            // and we send another request with the same refresh token as the previous one, and since we deleted it, it doesn't exist
            if (!_usersRefreshTokens.TryGetValue(request.RefreshToken, out RefreshTokenDTO existingRefreshToken))
            {
                throw new SecurityTokenException("Invalid token, login again."); // The token has expired
            }
            // Unauthenticating both user, this could happen if someone stoled access token (aleksa.trivan), and has own valid refresh token (filip.trivan), he could indefinedly generate access tokens for the (aleksa.trivan) then
            // This is not solving this problem (hacker can not change claims in the jwt token): https://stackoverflow.com/questions/27301557/if-you-can-decode-jwt-how-are-they-secure we are doing that with Decoding JWT token.
            // It is not posible for the user to change the email of the refresh token, even if it is, if the user change the email in the refresh token, it doesn't matter, we will find based on the refresh token code not email
            if (existingRefreshToken.Email != dbUserEmail) // FT: Could happen if someone gives me access and refresh for different users, i don't know which of these he stole so i unauthenticate both
            {
                RemoveRefreshTokenByEmail(existingRefreshToken.Email);
                RemoveRefreshTokenByEmail(dbUserEmail);
                throw new SecurityTokenException("Invalid token, login again.");
            }
            if (SettingsProvider.Current.AllowTheUseOfAppWithDifferentIpAddresses == false && IsRefreshTokenWithNewIpAddress(existingRefreshToken.Email, existingRefreshToken.IpAddress) == true)
            {
                // cuvas device-ove koje je cesto korisio, guras ih u familiju uredjaja, po nekom algoritmu odredi neki koji ti se cini sumnjiv i
                // na njemu mu trazi multifaktor aut. ako je klijent uopste trazio multifaktor
                RemoveRefreshTokenByEmail(existingRefreshToken.Email); // Don't need to delete for userDTO also, because we already did that
                throw new SecurityTokenException("Please login again, you can't use the application with two different IP addresses at the same time.");
            }

            return GenerateAccessAndRefreshTokens(dbUserEmail, principalClaims, existingRefreshToken.IpAddress, request.BrowserId, dbUserId); // need to recover the original claims
        }

        public RefreshTokenDTO ValidateAndGetVerificationTokenDTO(string verificationToken)
        {
            RemoveExpiredRefreshTokens();

            if (!_usersVerificationTokens.TryGetValue(verificationToken, out RefreshTokenDTO validVerificationToken))
            {
                throw new ExpiredVerificationException("The verification link has expired."); // We can not give allow user to send again from here, because it is deleted
            }
            KeyValuePair<string, RefreshTokenDTO> lastVerificationToken = _usersVerificationTokens.Where(x => x.Value.Email == validVerificationToken.Email).LastOrDefault();
            if (verificationToken != lastVerificationToken.Key)
            {
                throw new ExpiredVerificationException("The verification link has expired. Please, use the latest verification link.");
            }

            return validVerificationToken;
        }

        /// <summary>
        /// Password and verificationExpiration (minutes) are only needed if we are registering the account, for email verification
        /// </summary>
        public JwtAuthResultDTO GenerateAccessAndRefreshTokens(string userEmail, List<Claim> claims, string ipAddress, string browserId, long userId)
        {
            string accessToken = GenerateAccessToken(claims);
            RefreshTokenDTO refreshTokenDTO = HandleTokenDTO(userEmail, ipAddress, browserId);
            RemoveTheLastOneRefreshTokenFromTheSameBrowserAndEmail(browserId, userEmail); // And email also, because the one man can be logged in on the same browser as multiple users
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

        public JwtAuthResultDTO GenerateAccessAndRegistrationVerificationTokens(string userEmail, List<Claim> claims, int verificationExpiration, string password)
        {
            string accessToken = GenerateAccessToken(claims, verificationExpiration);
            RefreshTokenDTO registrationVerificationTokenDTO = HandleTokenDTO(userEmail, null, null, verificationExpiration, password);
            // It will always generate new token,
            // it is beneficial if the user open the application from different devices
            // if the user open the application on the multiple tabs in the same browser, we are working with the local storage so it will not make the difference
            _usersVerificationTokens.AddOrUpdate(registrationVerificationTokenDTO.TokenString, registrationVerificationTokenDTO, (_, _) => registrationVerificationTokenDTO);
            return new JwtAuthResultDTO
            {
                UserEmail = userEmail,
                AccessToken = accessToken,
                Token = registrationVerificationTokenDTO
            };
        }

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

        private RefreshTokenDTO HandleTokenDTO(string userEmail, string ipAddress, string browserId, int? verificationExpiration = null, string password = null)
        {
            RefreshTokenDTO refreshTokenDTO = new RefreshTokenDTO
            {
                Email = userEmail,
                IpAddress = ipAddress,
                BrowserId = browserId,
                TokenString = GenerateRandomTokenString(),
                ExpireAt = DateTime.Now.AddMinutes(verificationExpiration ?? SettingsProvider.Current.RefreshTokenExpiration),
                Password = password
            };

            return refreshTokenDTO;
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
        /// </summary>
        public (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new SecurityTokenException("Invalid token.");

            byte[] secretKey = Encoding.UTF8.GetBytes(SettingsProvider.Current.JwtKey);

            ClaimsPrincipal principal = new JwtSecurityTokenHandler()
                .ValidateToken(token,
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

        public RefreshTokenDTO GetLastVerificationTokenForTheEmail(string email)
        {
            return _usersVerificationTokens.Where(x => x.Value.Email == email && x.Value.Password != null).LastOrDefault().Value;
        }

        /// <summary>
        /// If we find it we will always remove only one token here
        /// </summary>
        public void RemoveTheLastOneRefreshTokenFromTheSameBrowserAndEmail(string browserId, string email)
        {
            KeyValuePair<string, RefreshTokenDTO> refreshToken = _usersRefreshTokens.Where(x => x.Value.BrowserId == browserId && x.Value.Email == email).FirstOrDefault();
            if (!string.IsNullOrEmpty(refreshToken.Key))
            {
                _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
            }
        }

        /// <summary>
        /// </summary>
        public void RemoveVerificationTokensByEmail(string email)
        {
            var verificationTokens = _usersVerificationTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var verificationToken in verificationTokens)
            {
                _usersVerificationTokens.TryRemove(verificationToken.Key, out _);
            }
        }

        // optional: clean up expired refresh tokens
        public void RemoveExpiredRefreshTokens()
        {
            var expiredTokens = _usersRefreshTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
            {
                _usersRefreshTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        public void RemoveExpiredVerificationTokens()
        {
            var expiredTokens = _usersVerificationTokens.Where(x => x.Value.ExpireAt < DateTime.Now).ToList();
            foreach (var expiredToken in expiredTokens)
            {
                _usersVerificationTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        // can be more specific to ip, user agent, device name, etc.
        public void RemoveRefreshTokenByEmail(string email)
        {
            var refreshTokens = _usersRefreshTokens.Where(x => x.Value.Email == email).ToList();
            foreach (var refreshToken in refreshTokens)
            {
                _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
            }
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
            if (_usersRefreshTokens.Where(x => x.Value.Email == email).LastOrDefault().Value.IpAddress != ipAddress)
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

    }
}
