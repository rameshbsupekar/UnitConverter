using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnitConverter.UnitsDefinitions.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnitIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unit_IsActive",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Unit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Unit",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unit_IsActive",
                table: "Unit",
                column: "IsActive");
        }
    }
}
