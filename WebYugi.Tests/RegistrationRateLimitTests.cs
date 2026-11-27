using Microsoft.AspNetCore.RateLimiting;
using WebYugi.Controllers;
using Xunit;

namespace WebYugi.Tests;

public sealed class RegistrationRateLimitTests
{
    [Fact]
    public void PublicRegistrationUsesTheAuthenticationRateLimit()
    {
        var action = typeof(UsersController).GetMethod(nameof(UsersController.Create));

        Assert.NotNull(action);
        var rateLimit = Assert.Single(action!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), false))
            as EnableRateLimitingAttribute;
        Assert.Equal("auth", rateLimit?.PolicyName);
    }
}
