using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OpsPulse.Domain.Common;
using OpsPulse.Infrastructure.Identity;

namespace OpsPulse.Tests.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "unit-test-signing-key-0123456789abcdef0123456789abcdef"
        }));

    [Fact]
    public void AccessToken_contains_identity_claims_and_future_expiry()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var (token, expires) = service.CreateAccessToken(
            userId, "jane@acme.io", "Jane Doe", organizationId, RoleNames.OrganizationAdmin);

        Assert.True(expires > DateTime.UtcNow);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal(userId.ToString(), jwt.GetClaim("sub").Value);
        Assert.Equal("jane@acme.io", jwt.GetClaim("email").Value);
        Assert.Equal(organizationId.ToString(), jwt.GetClaim("org").Value);
        Assert.Equal(RoleNames.OrganizationAdmin, jwt.GetClaim("role").Value);
    }

    [Fact]
    public void AccessToken_for_platform_admin_has_no_organization_claim()
    {
        var (token, _) = CreateService().CreateAccessToken(
            Guid.NewGuid(), "admin@opspulse.dev", "Admin", null, RoleNames.SuperAdmin);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.False(jwt.TryGetClaim("org", out _));
        Assert.Equal(RoleNames.SuperAdmin, jwt.GetClaim("role").Value);
    }

    [Fact]
    public void Refresh_token_is_opaque_and_hash_is_deterministic()
    {
        var service = CreateService();

        var (refreshToken, expires) = service.CreateRefreshToken();

        Assert.True(expires > DateTime.UtcNow.AddDays(6));
        Assert.Equal(64, JwtTokenService.HashToken(refreshToken).Length);
        Assert.Equal(JwtTokenService.HashToken(refreshToken), JwtTokenService.HashToken(refreshToken));
        Assert.NotEqual(refreshToken, JwtTokenService.HashToken(refreshToken));
    }
}
