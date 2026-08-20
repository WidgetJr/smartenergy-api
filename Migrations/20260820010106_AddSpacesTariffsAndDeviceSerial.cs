using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEnergy.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacesTariffsAndDeviceSerial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Homes_HomeId",
                table: "Devices");

            migrationBuilder.RenameColumn(
                name: "HomeId",
                table: "Devices",
                newName: "SpaceId");

            migrationBuilder.RenameColumn(
                name: "DeviceCode",
                table: "Devices",
                newName: "SerialNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_HomeId",
                table: "Devices",
                newName: "IX_Devices_SpaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_DeviceCode",
                table: "Devices",
                newName: "IX_Devices_SerialNumber");

            migrationBuilder.CreateTable(
                name: "EnergyTariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePerKWh = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyTariffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnergyTariffs_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Spaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Spaces_Homes_HomeId",
                        column: x => x.HomeId,
                        principalTable: "Homes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyTariffs_HomeId_EffectiveFrom",
                table: "EnergyTariffs",
                columns: new[] { "HomeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Spaces_HomeId",
                table: "Spaces",
                column: "HomeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Spaces_SpaceId",
                table: "Devices",
                column: "SpaceId",
                principalTable: "Spaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Spaces_SpaceId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "EnergyTariffs");

            migrationBuilder.DropTable(
                name: "Spaces");

            migrationBuilder.RenameColumn(
                name: "SpaceId",
                table: "Devices",
                newName: "HomeId");

            migrationBuilder.RenameColumn(
                name: "SerialNumber",
                table: "Devices",
                newName: "DeviceCode");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_SpaceId",
                table: "Devices",
                newName: "IX_Devices_HomeId");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_SerialNumber",
                table: "Devices",
                newName: "IX_Devices_DeviceCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Homes_HomeId",
                table: "Devices",
                column: "HomeId",
                principalTable: "Homes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
