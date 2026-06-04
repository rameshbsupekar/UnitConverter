using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnitConverter.UserManagement.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRevokedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: index already present in baseline schema for existing databases.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: reversible index drop is not required for this placeholder migration.
        }
    }
}
