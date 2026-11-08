using OpsPulse.Domain.Organizations;

namespace OpsPulse.Tests.Domain;

public class OrganizationTests
{
    [Fact]
    public void New_organization_gets_generated_id_and_is_active_by_default()
    {
        var organization = new Organization { Name = "Acme Corp", Slug = "acme-corp" };

        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.True(organization.IsActive);
        Assert.True((DateTime.UtcNow - organization.CreatedAtUtc).TotalSeconds < 5);
    }

    [Fact]
    public void New_organization_starts_with_empty_monitored_services_collection()
    {
        var organization = new Organization();

        Assert.Empty(organization.MonitoredServices);
    }
}
