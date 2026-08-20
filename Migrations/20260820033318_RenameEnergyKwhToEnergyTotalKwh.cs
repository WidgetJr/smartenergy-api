using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEnergy.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameEnergyKwhToEnergyTotalKwh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnergyKwh",
                table: "EnergyReadings",
                newName: "EnergyTotalKwh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnergyTotalKwh",
                table: "EnergyReadings",
                newName: "EnergyKwh");
        }
    }
}
