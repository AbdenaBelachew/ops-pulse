using OpsPulse.Domain.Services;

namespace OpsPulse.Tests.Domain;

public class MonitoredServiceTests
{
    [Fact]
    public void New_monitored_service_has_sensible_check_defaults()
    {
        var service = new MonitoredService();

        Assert.Equal(60, service.CheckIntervalSeconds);
        Assert.Equal(10, service.TimeoutSeconds);
        Assert.Equal("200", service.ExpectedStatusCodes);
        Assert.Equal(ServiceEnvironment.Production, service.Environment);
        Assert.True(service.IsActive);
    }
}
