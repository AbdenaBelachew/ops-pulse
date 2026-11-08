using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpsPulse.Domain.Common;
using OpsPulse.Infrastructure.Identity;

namespace OpsPulse.Infrastructure.Seed;

public static class DbSeeder
{
    /// <summary>
    /// Idempotent seeding: creates platform roles and the SuperAdmin account
    /// configured under <c>Seed:SuperAdmin</c>.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OpsPulse.DbSeeder");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Created role {Role}", role);
            }
        }

        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["Seed:SuperAdmin:Email"];
        var password = configuration["Seed:SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Seed:SuperAdmin is not configured — skipping SuperAdmin seed");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Platform Administrator",
            OrganizationId = null,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);
            logger.LogInformation("Seeded SuperAdmin account {Email}", email);
        }
        else
        {
            logger.LogError(
                "Failed to seed SuperAdmin: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
