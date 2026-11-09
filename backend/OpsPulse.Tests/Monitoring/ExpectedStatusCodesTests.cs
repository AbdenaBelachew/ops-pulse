using OpsPulse.Application.Services;
using OpsPulse.Infrastructure.Monitoring;

namespace OpsPulse.Tests.Monitoring;

public class ExpectedStatusCodesTests
{
    [Theory]
    [InlineData("200", 200, true)]
    [InlineData("200", 302, false)]
    [InlineData("200,204", 204, true)]
    [InlineData("200-299", 250, true)]
    [InlineData("200-299", 302, false)]
    [InlineData("200-399", 302, true)]
    [InlineData("200-299,301", 301, true)]
    [InlineData("200-299,301", 404, false)]
    public void Matches_codes_and_ranges(string expected, int code, bool matches)
    {
        Assert.True(SafeHttpCheckEngine.TryParseExpectedCodes(expected, out var predicate));
        Assert.Equal(matches, predicate(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("299-200")]
    [InlineData("200-700")]
    [InlineData("200-300-400")]
    public void Rejects_invalid_lists(string expected)
    {
        Assert.False(SafeHttpCheckEngine.TryParseExpectedCodes(expected, out _));
    }

    [Theory]
    [InlineData("200", true)]
    [InlineData("200,204", true)]
    [InlineData("200-399", true)]
    [InlineData("200-299,301,302", true)]
    [InlineData("2xx", false)]
    [InlineData("200,", false)]
    public void Validator_pattern_accepts_ranges(string expected, bool valid)
    {
        Assert.Equal(valid, System.Text.RegularExpressions.Regex.IsMatch(expected, ExpectedStatusCodesFormat.Pattern));
    }
}
