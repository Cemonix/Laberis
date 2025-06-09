using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace server.Authentication;

public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _configuration.GetValue<string>("Authentication:FakeUser:Id") ?? "auth0|default-debug-user"),
            new Claim(ClaimTypes.Name, _configuration.GetValue<string>("Authentication:FakeUser:Username") ?? "debugger"),
            new Claim(ClaimTypes.Email, _configuration.GetValue<string>("Authentication:FakeUser:Email") ?? "debug@example.com"),
            new Claim(ClaimTypes.Role, _configuration.GetValue<string>("Authentication:FakeUser:Roles") ?? "Admin")
        };

        var identity = new ClaimsIdentity(claims, "Fake");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "FakeScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}