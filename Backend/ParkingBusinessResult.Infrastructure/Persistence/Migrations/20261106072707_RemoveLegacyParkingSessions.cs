using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingBusinessResult.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyParkingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dev_parking_sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dev_parking_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    parkingLotId = table.Column<string>(type: "TEXT", nullable: false),
                    priceRuleId = table.Column<string>(type: "TEXT", nullable: true),
                    chargedAmount = table.Column<double>(type: "REAL", nullable: true),
                    enteredAt = table.Column<string>(type: "TEXT", nullable: false),
                    exitedAt = table.Column<string>(type: "TEXT", nullable: true),
                    paymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                    testPlate = table.Column<string>(type: "TEXT", nullable: false),
                    vehicleType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dev_parking_sessions", x => x.id);
                    table.CheckConstraint("CK_dev_parking_sessions_0", "julianday(\"enteredAt\") IS NOT NULL");
                    table.CheckConstraint("CK_dev_parking_sessions_1", "\"exitedAt\" IS NULL OR (julianday(\"exitedAt\") IS NOT NULL AND julianday(\"exitedAt\") > julianday(\"enteredAt\"))");
                    table.CheckConstraint("CK_dev_parking_sessions_2", "(\"exitedAt\" IS NULL AND \"priceRuleId\" IS NULL AND \"chargedAmount\" IS NULL AND \"paymentMethod\" IS NULL)\n        OR (\"exitedAt\" IS NOT NULL AND \"priceRuleId\" IS NOT NULL AND \"chargedAmount\" IS NOT NULL\n            AND \"chargedAmount\" >= 0 AND \"paymentMethod\" IS NOT NULL AND \"paymentMethod\" IN ('CASH', 'ELECTRONIC'))");
                    table.ForeignKey(
                        name: "FK_dev_parking_sessions_DL_KTHT_baiDoXe_parkingLotId",
                        column: x => x.parkingLotId,
                        principalTable: "DL_KTHT_baiDoXe",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_dev_parking_sessions_SP_chiTietMucThuKhungGia_priceRuleId",
                        column: x => x.priceRuleId,
                        principalTable: "SP_chiTietMucThuKhungGia",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_dev_parking_sessions_parkingLotId",
                table: "dev_parking_sessions",
                column: "parkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_dev_parking_sessions_priceRuleId",
                table: "dev_parking_sessions",
                column: "priceRuleId");
        }
    }
}
