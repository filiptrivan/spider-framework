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
using Spider.Security.DTO;
using System.Net;

namespace Spider.Security.Interface
{
    // TODO FT: Sort the arguments of the methods
    public interface IJwtAuthManager
    {
        IImmutableDictionary<string, RefreshTokenDTO> UsersRefreshTokensReadOnlyDictionary { get; }
        IImmutableDictionary<string, RegistrationVerificationTokenDTO> UsersRegistrationVerificationTokensReadOnlyDictionary { get; }
        IImmutableDictionary<string, LoginVerificationTokenDTO> UsersLoginVerificationTokensReadOnlyDictionary { get; }

        // Refresh
        JwtAuthResultDTO GenerateAccessAndRefreshTokens(long userId, string userEmail, string ipAddress, string browserId);
        List<Claim> GenerateClaims(long userId, string userEmail);
        JwtAuthResultDTO Refresh(RefreshTokenRequestDTO request, long dbUserId, string dbUserEmail);
        List<Claim> GetClaimsForTheAccessToken(RefreshTokenRequestDTO request, string accessToken);
        void RemoveExpiredRefreshTokens();
        void RemoveRefreshTokenByEmail(string email);
        public void Logout(string browserId, string email);
        bool RemoveLastRefreshTokenFromTheSameBrowserAndEmail(string browserId, string email);

        // Login verification
        LoginVerificationTokenDTO ValidateAndGetLoginVerificationTokenDTO(string verificationToken, string browserId, string email);
        string GenerateAndSaveLoginVerificationCode(string userEmail, long userId, string browserId);
        void RemoveLoginVerificationTokensByEmail(string email);

        // Registration verification
        RegistrationVerificationTokenDTO ValidateAndGetRegistrationVerificationTokenDTO(string verificationToken, string browserId, string email);
        string GenerateAndSaveRegistrationVerificationCode(string userEmail, string browserId);
        void RemoveRegistrationVerificationTokensByEmail(string email);
    }
}