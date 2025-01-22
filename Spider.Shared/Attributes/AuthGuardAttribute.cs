using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Spider.Shared.Attributes
{
    public class AuthGuardAttribute : ActionFilterAttribute
    {

        public AuthGuardAttribute()
        {
        }

        public async override void OnActionExecuting(ActionExecutingContext context)
        {
            string accessToken = await context.HttpContext.GetTokenAsync("Bearer", "access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!ValidateJwtToken(accessToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            base.OnActionExecuting(context);
        }

        private static bool ValidateJwtToken(string accessToken)
        {
            try
            {
                byte[] secretKey = Encoding.UTF8.GetBytes(SettingsProvider.Current.JwtKey);
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = SettingsProvider.Current.JwtIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ValidAudience = SettingsProvider.Current.JwtAudience,
                    ValidateAudience = true, // Checking if the audience is the valid one (localhost:7260)
                    ValidateLifetime = true, // If the token has expired, it will not be valid
                    ClockSkew = TimeSpan.FromMinutes(SettingsProvider.Current.ClockSkewMinutes),
                }, out SecurityToken validatedToken);

                //JwtSecurityToken jwtToken = validatedToken as JwtSecurityToken;
                //Optionally, check claims from token...
                //var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}