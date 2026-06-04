using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnitConverter.UnitsDefinitions.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitApprovalAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Unit",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Unit",
                type: "TEXT",
                precision: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Unit",
                type: "TEXT",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Unit",
                type: "TEXT",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Unit",
                type: "TEXT",
                precision: 6,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Unit",
                type: "TEXT",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Unit",
                type: "TEXT",
                precision: 6,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Unit",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Unit",
                type: "TEXT",
                precision: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedBy",
                table: "Unit",
                type: "TEXT",
                maxLength: 254,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unit_ApprovalStatus",
                table: "Unit",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_DimensionId_ApprovalStatus",
                table: "Unit",
                columns: new[] { "DimensionId", "ApprovalStatus" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Unit_ApprovalStatus",
                table: "Unit",
                sql: "ApprovalStatus >= 0 AND ApprovalStatus <= 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unit_ApprovalStatus",
                table: "Unit");

            migrationBuilder.DropIndex(
                name: "IX_Unit_DimensionId_ApprovalStatus",
                table: "Unit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Unit_ApprovalStatus",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Unit");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "Unit");
        }
    }
}
