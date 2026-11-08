using FluentValidation;
using OpsPulse.Application.Services;

namespace OpsPulse.Tests.Monitoring;

public class CreateServiceRequestValidatorTests
{
    private readonly CreateServiceRequestValidator _validator = new();

    private static OpsPulse.Application.Services.CreateServiceRequest Valid(
        string url = "https://api.example.com/health",
        string codes = "200") => new("Payments API", url, null,
            OpsPulse.Domain.Services.ServiceEnvironment.Production, 60, 10, codes);

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await _validator.ValidateAsync(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com/health")]
    [InlineData("")]
    public async Task Non_http_urls_fail(string url)
    {
        var result = await _validator.ValidateAsync(Valid(url: url));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("20")]
    [InlineData("2000")]
    [InlineData("200,abc")]
    public async Task Malformed_status_code_lists_fail(string codes)
    {
        var result = await _validator.ValidateAsync(Valid(codes: codes));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Multiple_codes_pass()
    {
        var result = await _validator.ValidateAsync(Valid(codes: "200,204,301"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Interval_bounds_are_enforced()
    {
        Assert.False((await _validator.ValidateAsync(new OpsPulse.Application.Services.CreateServiceRequest(
            "S", "https://x.dev", null, OpsPulse.Domain.Services.ServiceEnvironment.Production, 5, 10, "200"))).IsValid);

        Assert.True((await _validator.ValidateAsync(new OpsPulse.Application.Services.CreateServiceRequest(
            "S", "https://x.dev", null, OpsPulse.Domain.Services.ServiceEnvironment.Production, 3600, 10, "200"))).IsValid);
    }
}
