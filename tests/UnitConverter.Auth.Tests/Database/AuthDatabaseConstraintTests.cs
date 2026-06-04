using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;
using UnitConverter.UserManagement.DataAccess.Data;
namespace UnitConverter.UserManagement.Api.Tests.Database;

[TestClass]
public sealed class AuthDatabaseConstraintTests
{
    [TestMethod]
    public async Task DuplicateEmail_ViolatesUniqueIndex()
    {
        await using var db = await CreateMigratedContextAsync();

        db.Users.Add(User.Create(1, "a@test.local", "A", "User", "Org", "hash"));
        await db.SaveChangesAsync();

        db.Users.Add(User.Create(2, "a@test.local", "B", "User", "Org", "hash"));

        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public async Task RefreshToken_WithoutUser_ViolatesForeignKey()
    {
        await using var db = await CreateMigratedContextAsync();

        db.RefreshTokens.Add(RefreshToken.Create(
            id: 1,
            userId: UserId.Create(999),
            jwtId: "jti-1",
            tokenValue: "refresh-token-value",
            expiresAt: DateTime.UtcNow.AddDays(7)));

        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public async Task UserRole_ReferencesRole_WithRestrictDelete()
    {
        await using var db = await CreateMigratedContextAsync();

        var role = Role.Create(99, "TestRole", "Test");
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var user = User.Create(10, "role@test.local", "R", "User", "Org", "hash");
        user.AssignRole(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
        {
            db.Roles.Remove(role);
            return db.SaveChangesAsync();
        });
    }

    private static async Task<AuthDbContext> CreateMigratedContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AuthDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }
}
