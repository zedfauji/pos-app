using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Internal;
using System.Text.Encodings.Web;

namespace MagiDesk.Shared.Authorization.Authentication;

/// <summary>
/// No-op authentication handler for APIs that use custom authorization via headers
/// This handler doesn't actually authenticate but satisfies ASP.NET Core's requirement for an authentication scheme
/// </summary>
public class NoOpAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public NoOpAuthenticationHandler(
        IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Return NoResult - we don't authenticate here, authorization is handled by custom middleware
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
    }
}

