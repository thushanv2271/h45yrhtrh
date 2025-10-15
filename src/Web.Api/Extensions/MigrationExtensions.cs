using Infrastructure.Database;
using Infrastructure.Database.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }

    public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        // Try to get the seeder, but don't fail if it's not registered (e.g., in tests)
        DatabaseSeeder? seeder = scope.ServiceProvider.GetService<DatabaseSeeder>();

        if (seeder != null)
        {
            await seeder.SeedAsync();
        }
    }
}
