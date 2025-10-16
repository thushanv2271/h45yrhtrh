using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Branches;
using Domain.MasterData;
using Domain.Organizations;
using Domain.Permissions;
using Domain.RolePermissions;
using Domain.Roles;
using Domain.UserRoles;
using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SharedKernel;

namespace Infrastructure.Database.Seeding;

public sealed class DatabaseSeeder(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ILogger<DatabaseSeeder> logger)
{
    private const string AdministratorRoleName = "Administrator";
    private const string AdminEmail = "admin@saral.com";
    private const string AdminPass = "Admin123!";
    private readonly string AdminPasswordHash = passwordHasher.Hash(AdminPass);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database seeding...");

        await SeedPermissionsAsync(cancellationToken);
        await SeedOrganizationsAsync(cancellationToken);
        await SeedBranchesAsync(cancellationToken);
        await SeedAdministratorRoleAndUserAsync(cancellationToken);
        await SeedSegmentMasterAsync(cancellationToken);

        logger.LogInformation("Database seeding completed successfully.");
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding permissions...");

        HashSet<string> existingPermissions = await context.Permissions
            .Select(p => p.Key)
            .ToHashSetAsync(cancellationToken);

        var permissionsToAdd = PermissionRegistry.GetAllPermissions()
            .Where(permissionDef => !existingPermissions.Contains(permissionDef.Key))
            .Select(permissionDef => new Permission(
                Guid.CreateVersion7(),
                permissionDef.Key,
                permissionDef.DisplayName,
                permissionDef.Category,
                permissionDef.Description))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            context.Permissions.AddRange(permissionsToAdd);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Added {Count} new permissions", permissionsToAdd.Count);
        }
        else
        {
            logger.LogInformation("All permissions already exist, skipping permission seeding");
        }
    }

    private async Task SeedAdministratorRoleAndUserAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding administrator role and user...");

        // Create Administrator role if it doesn't exist
        Role? adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == AdministratorRoleName, cancellationToken);

        Branch? defaultBranch = await context.Branches
            .FirstOrDefaultAsync(b => b.BranchCode == "CMB001", cancellationToken);

        // Create or update admin user if it doesn't exist or lacks branch
        User? adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == AdminEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new User
            {
                Id = Guid.CreateVersion7(),
                Email = AdminEmail,
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = AdminPasswordHash,
                BranchId = defaultBranch?.Id, // Assign to default branch
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            logger.LogInformation("Created admin user: {Email} assigned to branch: {BranchCode}",
                AdminEmail, defaultBranch?.BranchCode ?? "None");

            await context.SaveChangesAsync(cancellationToken);
        }
        else if (adminUser.BranchId == null && defaultBranch != null)
        {
            // Update existing admin user with branch
            adminUser.BranchId = defaultBranch.Id;
            adminUser.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated admin user with branch: {BranchCode}", defaultBranch.BranchCode);
        }

        if (adminRole is null)
        {
            adminRole = new Role(
                Guid.CreateVersion7(),
                AdministratorRoleName,
                "System administrator with full access to all features",
                isSystemRole: true);

            context.Roles.Add(adminRole);
            logger.LogInformation("Created Administrator role");
            await context.SaveChangesAsync(cancellationToken);
        }

        // Assign all permissions to Administrator role
        List<Permission> allPermissions = await context.Permissions.ToListAsync(cancellationToken);
        List<Guid> existingRolePermissionIds = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var newRolePermissions = allPermissions
            .Where(p => !existingRolePermissionIds.Contains(p.Id))
            .Select(p => new RolePermission(adminRole.Id, p.Id))
            .ToList();

        if (newRolePermissions.Count > 0)
        {
            context.RolePermissions.AddRange(newRolePermissions);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned {Count} permissions to Administrator role", newRolePermissions.Count);
        }

        // Assign Administrator role to admin user if not already assigned
        bool hasAdminRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id, cancellationToken);

        if (!hasAdminRole)
        {
            UserRole userRole = new(adminUser.Id, adminRole.Id);
            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned Administrator role to admin user");
        }
    }

    private async Task SeedSegmentMasterAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding SegmentMaster data...");

        bool hasSegments = await context.SegmentMasters.AnyAsync(cancellationToken);
        if (hasSegments)
        {
            logger.LogInformation("SegmentMaster data already exists, skipping seeding.");
            return;
        }

        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string excelPath = config["SegmentMasterDataPath"];
        if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
        {
            logger.LogWarning("SegmentMasterDataPath not found or file missing: {Path}", excelPath);
            return;
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using ExcelPackage package = new(excelPath);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        Dictionary<string, List<string>> segmentDict = new();

        int rowCount = worksheet.Dimension.Rows;
        for (int row = 2; row <= rowCount; row++)
        {
            string segment = worksheet.Cells[row, 1].Text.Trim();
            string subsegment = worksheet.Cells[row, 2].Text.Trim();

            if (string.IsNullOrWhiteSpace(segment) || string.IsNullOrWhiteSpace(subsegment))
            {
                continue;
            }

            if (!segmentDict.TryGetValue(segment, out List<string> subSegments))
            {
                subSegments = new List<string>();
                segmentDict[segment] = subSegments;
            }

            subSegments.Add(subsegment);
        }

        var entities = segmentDict
            .Select(kvp => new SegmentMaster
            {
                Id = Guid.CreateVersion7(),
                Segment = kvp.Key,
                SubSegments = kvp.Value,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            })
            .ToList();

        context.SegmentMasters.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} SegmentMaster records.", entities.Count);
    }

    //Seed Organizations Data  
    private async Task SeedOrganizationsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Organizations data...");

        bool hasOrganizations = await context.Organizations.AnyAsync(cancellationToken);
        if (hasOrganizations)
        {
            logger.LogInformation("Organizations data already exists, skipping seeding.");
            return;
        }

        var organizations = new List<Organization>
    {
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Azend Technologies",
            Code = "AZEND",
            Email = "info@azendtech.com",
            ContactNumber = "+94 71 234 5678",
            Address = "123 Galle Road, Colombo 03, Sri Lanka",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Cora Analytics",
            Code = "CORA",
            Email = "contact@cora.com",
            ContactNumber = "+44 20 7946 1234",
            Address = "45 Baker Street, London, UK",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Global Tech Solutions",
            Code = "GLOBAL",
            Email = "info@globaltech.com",
            ContactNumber = "+1 555 123 4567",
            Address = "500 Tech Park, San Francisco, CA, USA",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Pacific Innovations",
            Code = "PACIFIC",
            Email = "hello@pacificinno.com",
            ContactNumber = "+61 2 9876 5432",
            Address = "88 Harbour Boulevard, Sydney, Australia",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Nordic Systems",
            Code = "NORDIC",
            Email = "support@nordicsys.com",
            ContactNumber = "+46 8 123 4567",
            Address = "15 Storgatan, Stockholm, Sweden",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Asia Pacific Ventures",
            Code = "APAC",
            Email = "contact@apacventures.com",
            ContactNumber = "+65 6789 1234",
            Address = "Marina Bay Tower, Singapore",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Emerald Industries",
            Code = "EMERALD",
            Email = "info@emeraldind.com",
            ContactNumber = "+353 1 234 5678",
            Address = "67 O'Connell Street, Dublin, Ireland",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Southern Cross Corp",
            Code = "SOCROSS",
            Email = "hello@southerncross.co.nz",
            ContactNumber = "+64 9 876 5432",
            Address = "120 Queen Street, Auckland, New Zealand",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    };

        context.Organizations.AddRange(organizations);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} Organizations.", organizations.Count);
    }
    //Seed Branches Data  
    private async Task SeedBranchesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Branches data...");

        bool hasBranches = await context.Branches.AnyAsync(cancellationToken);
        if (hasBranches)
        {
            logger.LogInformation("Branches data already exists, skipping seeding.");
            return;
        }

        // Get Azend Technologies organization
        Organization? azendOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Code == "AZEND", cancellationToken);

        if (azendOrg == null)
        {
            logger.LogWarning("Azend Technologies organization not found. Skipping branch seeding.");
            return;
        }

        var branches = new List<Branch>
        {
            new Branch
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = azendOrg.Id,
                BranchName = "Colombo Main Branch",
                BranchCode = "CMB001",
                Email = "colombo@azendtech.com",
                ContactNumber = "+94 11 222 3344",
                Address = "No. 45, Galle Road, Colombo",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = azendOrg.Id,
                BranchName = "Kandy Branch",
                BranchCode = "KDY002",
                Email = "kandy@azendtech.com",
                ContactNumber = "+94 81 223 4455",
                Address = "Peradeniya Road, Kandy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Branches.AddRange(branches);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} Branches.", branches.Count);
    }
}
