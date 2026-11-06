using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class DevAuthUsersConfiguration : IEntityTypeConfiguration<DevAuthUsers>
{
    public void Configure(EntityTypeBuilder<DevAuthUsers> entity)
    {
        entity.ToTable("dev_auth_users");

        entity.HasIndex(e => e.Username, "IX_dev_auth_users_username").IsUnique();

        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.ParkingLotId).HasColumnName("parkingLotId");
        entity.Property(e => e.PasswordHash).HasColumnName("passwordHash");
        entity.Property(e => e.Role).HasColumnName("role");
        entity.Property(e => e.Username)
            .UseCollation("NOCASE")
            .HasColumnName("username");
    }
}
