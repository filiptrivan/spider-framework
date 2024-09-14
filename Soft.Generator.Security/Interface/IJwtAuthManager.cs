using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Soft.Generator.Security.DTO;
using System.Net;

namespace Soft.Generator.Security.Interface
{
    public interface IJwtAuthManager
    {
        IImmutableDictionary<string, RefreshTokenDTO> UsersRefreshTokensReadOnlyDictionary { get; }

        /// <summary>
        /// Password is only needed if we are registering the account, for email verification, didn't want to make other (eg. VerificationToken) for this to stay consistent and don't add overhead
        /// </summary>
        JwtAuthResultDTO GenerateAccessAndRefreshTokens(string userEmail, List<Claim> claims, string ipAddress, string browserId, long userId);
        JwtAuthResultDTO GenerateAccessAndRegistrationVerificationTokens(string userEmail, List<Claim> claims, int verificationExpiration, string password);
        JwtAuthResultDTO Refresh(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims);
        RefreshTokenDTO ValidateAndGetVerificationTokenDTO(string verificationToken);
        List<Claim> GetPrincipalClaimsForAccessToken(RefreshTokenRequestDTO request, string accessToken);
        void RemoveExpiredRefreshTokens();
        void RemoveRefreshTokenByEmail(string email);
        (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token);
        RefreshTokenDTO GetLastVerificationTokenForTheEmail(string email);
        void RemoveTheLastOneRefreshTokenFromTheSameBrowserAndEmail(string browserId, string email);
        void RemoveVerificationTokensByEmail(string email);
    }
}