using Domain.CustomerExposures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.CustomerExposures;

/// <summary>
/// Configures the EF Core mapping for the <see cref="CustomerExposure"/> entity.
/// Defines keys, properties, indexes, and the table name.
/// </summary>
internal sealed class CustomerExposureConfiguration : IEntityTypeConfiguration<CustomerExposure>
{
    public void Configure(EntityTypeBuilder<CustomerExposure> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.CustomerId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.AmortizedCost)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.BranchId)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.AsOfDate)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Foreign key relationship with Branch
        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance - critical for the GROUP BY customer_id query
        builder.HasIndex(e => e.CustomerId)
            .HasDatabaseName("IX_CustomerExposures_CustomerId");

        builder.HasIndex(e => new { e.BranchId, e.AsOfDate, e.Currency })
            .HasDatabaseName("IX_CustomerExposures_BranchId_AsOfDate_Currency");

        // Composite index for the threshold summary query
        builder.HasIndex(e => new { e.CustomerId, e.BranchId, e.AsOfDate, e.Currency })
            .HasDatabaseName("IX_CustomerExposures_Summary_Query");

        // Table name (snake_case will be applied by EFCore.NamingConventions)
        builder.ToTable("customer_exposures");
    }
}
