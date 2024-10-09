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
        IImmutableDictionary<string, RegistrationVerificationTokenDTO> UsersRegistrationVerificationTokensReadOnlyDictionary { get; }
        IImmutableDictionary<string, LoginVerificationTokenDTO> UsersLoginVerificationTokensReadOnlyDictionary { get; }
        IImmutableDictionary<string, ForgotPasswordVerificationTokenDTO> UsersForgotPasswordVerificationTokensReadOnlyDictionary { get; }

        // Refresh
        JwtAuthResultDTO GenerateAccessAndRefreshTokens(string userEmail, List<Claim> claims, string ipAddress, string browserId, long userId);
        JwtAuthResultDTO Refresh(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims);
        JwtAuthResultDTO RefreshDevHack(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail, List<Claim> principalClaims);
        List<Claim> GetPrincipalClaimsForAccessToken(RefreshTokenRequestDTO request, string accessToken);
        void RemoveExpiredRefreshTokens();
        void RemoveRefreshTokenByEmail(string email);
        (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token);
        void RemoveTheLastRefreshTokenFromTheSameBrowserAndEmail(string browserId, string email);

        // Login verification
        LoginVerificationTokenDTO ValidateAndGetLoginVerificationTokenDTO(string verificationToken, string browserId, string email);
        string GenerateAndSaveLoginVerificationCode(string userEmail, long userId, string browserId);
        void RemoveLoginVerificationTokensByEmail(string email);

        // Forgot password verification
        ForgotPasswordVerificationTokenDTO ValidateAndGetForgotPasswordVerificationTokenDTO(string verificationToken, string browserId, string email);
        string GenerateAndSaveForgotPasswordVerificationCode(string userEmail, long userId, string newPassword, string browserId);
        void RemoveForgotPasswordVerificationTokensByEmail(string email);

        // Registration verification
        RegistrationVerificationTokenDTO ValidateAndGetRegistrationVerificationTokenDTO(string verificationToken, string browserId, string email);
        string GenerateAndSaveRegistrationVerificationCode(string userEmail, string password, string browserId);
        void RemoveRegistrationVerificationTokensByEmail(string email);
    }
}