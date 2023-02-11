// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Pomelo.Storage.WebDAV.Sample.Models;

namespace Pomelo.Storage.WebDAV.Sample.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                Response.Headers.Add("WWW-Authenticate", @"Basic realm=""server""");
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }

            string username = null;
            string password = null;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                username = credentials[0];
                password = credentials[1];
                var users = Program.Configuration.GetSection("Users").Get<List<User>>();
                if (!users.Any(x => x.Username == username && x.Password == password))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
                }
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public static class BasicAuthenticationHandlerExtensions
    {
        public static IServiceCollection AddBasicAuthenticationHandler(this IServiceCollection services)
        {
            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
            return services;
        }
    }
}
