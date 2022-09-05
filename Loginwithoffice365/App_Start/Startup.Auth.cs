using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Owin;
using Loginwithoffice365.Models;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Loginwithoffice365
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        //private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string aadInstance = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["ida:AADInstance"], tenantId);
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        public static readonly string Authority = aadInstance;

        private static string tenantId2 = ConfigurationManager.AppSettings["ida:TenantId2"];
        private static string aadInstance2 = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance2"]);
        public static readonly string Authority2 = aadInstance2 + tenantId2;

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        string graphResourceId = "https://graph.microsoft.com/";

        public void ConfigureAuth(IAppBuilder app)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    RedirectUri = postLogoutRedirectUri,
                    // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it is using the home page
                    PostLogoutRedirectUri = postLogoutRedirectUri,
                    Scope = OpenIdConnectScope.OpenIdProfile,

                    // ResponseType is set to request the code id_token - which contains basic information about the signed-in user
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                    // To only allow users from a single organizations, set ValidateIssuer to true and 'tenant' setting in web.config to the tenant name
                    // To allow users from only a list of specific organizations, set ValidateIssuer to true and use ValidIssuers parameter
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false // This is a simplification
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                       AuthorizationCodeReceived = (context) => 
                       {
                           var code = context.Code;
                           ClientCredential credential = new ClientCredential(clientId, appKey);
                           string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
                           AuthenticationContext authContext = new AuthenticationContext(Authority, new ADALTokenCache(signedInUserID));
                           //AuthenticationResult result = authContext.AcquireTokenByAuthorizationCodeAsync(
                           //    code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceId).Result;

                           return Task.FromResult(0);
                       }
                    }
                });
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
