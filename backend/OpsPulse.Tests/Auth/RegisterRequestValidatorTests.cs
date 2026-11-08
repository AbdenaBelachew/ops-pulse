using FluentValidation;
using OpsPulse.Application.Auth;

namespace OpsPulse.Tests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("Acme Corp", "Jane Doe", "jane@acme.io", "Str0ng!Passw0rd"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Aa1!aa")]          // too short
    [InlineData("weakpassword1!")]  // no uppercase
    [InlineData("WEAKPASSWORD1!")]  // no lowercase
    [InlineData("WeakPassword!!")]  // no digit
    [InlineData("WeakPassword11")]  // no special character
    public async Task Weak_passwords_fail(string password)
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("Acme Corp", "Jane Doe", "jane@acme.io", password));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Empty_organization_name_fails()
    {
        var result = await _validator.ValidateAsync(
            new RegisterRequest("", "Jane Doe", "jane@acme.io", "Str0ng!Passw0rd"));

        Assert.False(result.IsValid);
    }
}
