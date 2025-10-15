using Infrastructure.Database;
using IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using Xunit;

namespace IntegrationTests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient HttpClient;
    protected readonly ApplicationDbContext DbContext;
    protected readonly IServiceScope Scope;
    protected readonly Guid TestUserId;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        TestUserId = Guid.CreateVersion7();

        HttpClient = factory.CreateClient();
        AuthenticateAsAdminUser();

        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected void AuthenticateAsAdminUser()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");

        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", TestUserId.ToString());
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions",
            string.Join(",", GetAllPermissions()));
    }

    protected async Task AuthenticateAsUserWithoutPermissionsAsync()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");

        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", Guid.CreateVersion7().ToString());
        await Task.CompletedTask;
    }

    private static List<string> GetAllPermissions()
    {
        return new List<string>
        {
            PermissionRegistry.AdminDashboardRead,
            PermissionRegistry.AdminUserManagementCreate,
            PermissionRegistry.AdminUserManagementRead,
            PermissionRegistry.AdminUserManagementEdit,
            PermissionRegistry.AdminUserManagementDelete,
            PermissionRegistry.AdminSettingsRolePermissionCreate,
            PermissionRegistry.AdminSettingsRolePermissionRead,
            PermissionRegistry.AdminSettingsRolePermissionEdit,
            PermissionRegistry.AdminSettingsRolePermissionDelete,
            PermissionRegistry.PDSetupAccess
        };
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        Scope.Dispose();
        HttpClient.Dispose();
    }
}
