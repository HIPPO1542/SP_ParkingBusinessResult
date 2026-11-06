using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class DevParkingSessionsConfiguration : IEntityTypeConfiguration<DevParkingSessions>
{
    public void Configure(EntityTypeBuilder<DevParkingSessions> entity)
    {
        entity.ToTable("dev_parking_sessions");

        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.ChargedAmount).HasColumnName("chargedAmount");
        entity.Property(e => e.EnteredAt).HasColumnName("enteredAt");
        entity.Property(e => e.ExitedAt).HasColumnName("exitedAt");
        entity.Property(e => e.ParkingLotId).HasColumnName("parkingLotId");
        entity.Property(e => e.PaymentMethod).HasColumnName("paymentMethod");
        entity.Property(e => e.PriceRuleId).HasColumnName("priceRuleId");
        entity.Property(e => e.TestPlate).HasColumnName("testPlate");
        entity.Property(e => e.VehicleType).HasColumnName("vehicleType");

        entity.HasOne(d => d.ParkingLot).WithMany(p => p.DevParkingSessions)
            .HasForeignKey(d => d.ParkingLotId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.PriceRule).WithMany(p => p.DevParkingSessions).HasForeignKey(d => d.PriceRuleId);
        entity.ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_0", "julianday(\"enteredAt\") IS NOT NULL"));
        entity.ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_1", "\"exitedAt\" IS NULL OR (julianday(\"exitedAt\") IS NOT NULL AND julianday(\"exitedAt\") > julianday(\"enteredAt\"))"));
        entity.ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_2", "(\"exitedAt\" IS NULL AND \"priceRuleId\" IS NULL AND \"chargedAmount\" IS NULL AND \"paymentMethod\" IS NULL)\n        OR (\"exitedAt\" IS NOT NULL AND \"priceRuleId\" IS NOT NULL AND \"chargedAmount\" IS NOT NULL\n            AND \"chargedAmount\" >= 0 AND \"paymentMethod\" IS NOT NULL AND \"paymentMethod\" IN ('CASH', 'ELECTRONIC'))"));
    }
}
