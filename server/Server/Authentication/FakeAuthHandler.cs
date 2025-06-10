using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using server.Configs;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace server.Authentication;

public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AdminUserSettings _fakeUserSettings;

    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AdminUserSettings> fakeUserSettings
    ) : base(options, logger, encoder)
    {
        _fakeUserSettings = fakeUserSettings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _fakeUserSettings.Id),
            new Claim(ClaimTypes.Name, _fakeUserSettings.Username),
            new Claim(ClaimTypes.Email, _fakeUserSettings.Email),
            new Claim(ClaimTypes.Role, _fakeUserSettings.Role)
        };

        var identity = new ClaimsIdentity(claims, "Fake");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "FakeScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}