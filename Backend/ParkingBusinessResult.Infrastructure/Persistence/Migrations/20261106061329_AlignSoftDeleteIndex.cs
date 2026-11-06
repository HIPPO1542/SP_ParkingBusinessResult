using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingBusinessResult.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSoftDeleteIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_active_business_result_lot_period",
                table: "SP_ketQuaKinhDoanhBaiDoXe");

            migrationBuilder.CreateIndex(
                name: "ux_active_business_result_lot_period",
                table: "SP_ketQuaKinhDoanhBaiDoXe",
                columns: new[] { "baiDoXeId", "kyBaoCao" },
                unique: true,
                filter: "\"daXoa\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_active_business_result_lot_period",
                table: "SP_ketQuaKinhDoanhBaiDoXe");

            migrationBuilder.CreateIndex(
                name: "ux_active_business_result_lot_period",
                table: "SP_ketQuaKinhDoanhBaiDoXe",
                columns: new[] { "baiDoXeId", "kyBaoCao" },
                unique: true,
                filter: "\"daXoa\" = 0");
        }
    }
}
