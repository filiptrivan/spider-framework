using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Spider.Shared.Helpers;

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

            if (!Helpers.Helpers.IsJwtTokenValid(accessToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            base.OnActionExecuting(context);
        }

    }
}