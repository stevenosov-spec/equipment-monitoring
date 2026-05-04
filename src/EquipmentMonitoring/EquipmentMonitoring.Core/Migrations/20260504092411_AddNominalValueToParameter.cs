using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentMonitoring.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNominalValueToParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "NominalValue",
                table: "Parameters",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Faults",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NominalValue",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Faults");
        }
    }
}
