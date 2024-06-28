using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Client.AspNetCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazingPizza.Server.Controllers
{
    [ApiController]
    [Route("callback/login/{provider}")]
    public class AuthController : ControllerBase
    {
        [HttpGet, HttpPost, IgnoreAntiforgeryToken]
        public async Task<IActionResult> LogInCallback()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal is not ClaimsPrincipal { Identity.IsAuthenticated: true })
            {
                throw new InvalidOperationException("The external authorization data cannot be used for authentication.");
            }

            var identity = new ClaimsIdentity("ExternalLogin");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            var nameIdentifier = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (email != null) identity.AddClaim(new Claim(ClaimTypes.Email, email));
            if (name != null) identity.AddClaim(new Claim(ClaimTypes.Name, name));
            if (nameIdentifier != null) identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
            var registrationId = result.Principal.FindFirstValue("registration_id");
            if (registrationId != null) identity.AddClaim(new Claim("registration_id", registrationId));

            var properties = new AuthenticationProperties(result.Properties.Items)
            {
                RedirectUri = result.Properties.RedirectUri ?? "/"
            };

            properties.StoreTokens(result.Properties.GetTokens().Where(token => token.Name is
                OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken or
                OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken or
                OpenIddictClientAspNetCoreConstants.Tokens.RefreshToken));

            return SignIn(new ClaimsPrincipal(identity), properties);
        }
    }
}