using Infrastructure.Database;
using Infrastructure.Database.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Web.Api;
using Xunit;

namespace IntegrationTests.Helpers;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention());

            // Remove database seeder to prevent conflicts in tests
            services.RemoveAll<DatabaseSeeder>();

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });

            // Override the default authentication scheme
            services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        // Start the container with a timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _dbContainer.StartAsync(cts.Token);

        // Wait a bit for the container to be fully ready
        await Task.Delay(2000);

        // Use using statement to ensure proper disposal of scope
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Seed only essential data (permissions) for tests
        await SeedTestDataAsync(dbContext);
    }

    private static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Seed permissions only (needed for authorization tests)
        HashSet<string> existingPermissions = await context.Permissions
            .Select(p => p.Key)
            .ToHashSetAsync();

        var permissionsToAdd = SharedKernel.PermissionRegistry.GetAllPermissions()
            .Where(permissionDef => !existingPermissions.Contains(permissionDef.Key))
            .Select(permissionDef => new Domain.Permissions.Permission(
                Guid.CreateVersion7(),
                permissionDef.Key,
                permissionDef.DisplayName,
                permissionDef.Category,
                permissionDef.Description))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            context.Permissions.AddRange(permissionsToAdd);
            await context.SaveChangesAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}
