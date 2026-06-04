using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnitConverter.UnitsDefinitions.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialUnitDimensionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitDimension",
                columns: table => new
                {
                    DimensionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitDimension", x => x.DimensionId);
                });

            migrationBuilder.CreateTable(
                name: "Unit",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DimensionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MultiplierToBase = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false, defaultValue: 1m),
                    OffsetToBase = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit", x => x.UnitId);
                    table.CheckConstraint("CK_Unit_MultiplierToBase_Positive", "MultiplierToBase > 0");
                    table.ForeignKey(
                        name: "FK_Unit_Dimension",
                        column: x => x.DimensionId,
                        principalTable: "UnitDimension",
                        principalColumn: "DimensionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Unit_DimensionId",
                table: "Unit",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_IsActive",
                table: "Unit",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UQ_Unit_Dimension_Symbol",
                table: "Unit",
                columns: new[] { "DimensionId", "Symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UnitDimension_Name",
                table: "UnitDimension",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Unit");

            migrationBuilder.DropTable(
                name: "UnitDimension");
        }
    }
}
