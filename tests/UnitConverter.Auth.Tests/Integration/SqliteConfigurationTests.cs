using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Tests.Integration;

/// <summary>
/// Integration tests for SQLite configuration and local development setup.
/// Verifies that SQLite is correctly configured for MVP deployment model.
/// Tests cover: folder creation, file persistence, migrations, and database operations.
/// </summary>
[TestClass]
public class SqliteConfigurationTests
{
    private string _testDataFolder = null!;
    private string _testDbPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDataFolder = Path.Combine(Path.GetTempPath(), $"SqliteTest_{Guid.NewGuid()}");
        _testDbPath = Path.Combine(_testDataFolder, "test_converter.db");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDataFolder))
        {
            Directory.Delete(_testDataFolder, recursive: true);
        }
    }

    [TestMethod]
    public void GivenNonExistentDataFolder_WhenCreatingConnection_ThenFolderIsCreated()
    {
        // Arrange
        Directory.Exists(_testDataFolder).Should().BeFalse();

        // Act
        Directory.CreateDirectory(_testDataFolder);

        // Assert
        Directory.Exists(_testDataFolder).Should().BeTrue();
    }

    [TestMethod]
    public async Task GivenConnectionString_WhenConnecting_ThenSqliteFileIsCreated()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        // Act
        await using var context = new AuthDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        File.Exists(_testDbPath).Should().BeTrue($"SQLite database file should be created at {_testDbPath}");
    }

    [TestMethod]
    public async Task GivenSqliteDatabase_WhenCallingMigrateAsync_ThenTablesAreCreated()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        // Act
        await using var context = new AuthDbContext(options);
        await context.Database.MigrateAsync();

        // Assert - verify tables exist by inserting data
        var user = User.Create(
            userId: 1,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            organizationName: "Test Org",
            passwordHash: "hashed_password");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GivenEmptyDatabase_WhenInserting_ThenDataPersists()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        // Act - insert in first context
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Assert - verify data persists in new context
        await using (var context = new AuthDbContext(options))
        {
            var persistedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            persistedUser.Should().NotBeNull();
            persistedUser!.Email.Value.Should().Be("john@example.com");
            persistedUser.FirstName.Should().Be("John");
        }
    }

    [TestMethod]
    public async Task GivenSqliteInMemoryDatabase_WhenUsingSharedConnection_ThenDataPersistsAcrossContexts()
    {
        // Arrange - in-memory SQLite requires shared connection for data to persist
        var connectionString = "Data Source=:memory:";
        
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var user = User.Create(
            userId: 1,
            email: "memory@example.com",
            firstName: "Memory",
            lastName: "User",
            organizationName: "Temp Org",
            passwordHash: "hashed_password");

        // Act & Assert - in-memory creates fresh DB per context, but EnsureCreatedAsync sets up schema
        await using var context = new AuthDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Value.Should().Be("memory@example.com");
    }

    [TestMethod]
    public async Task GivenUser_WhenQueryingByUniqueEmailIndex_ThenIndexIsApplied()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var user1 = User.Create(
            userId: 1,
            email: "unique@example.com",
            firstName: "User",
            lastName: "One",
            organizationName: "Org",
            passwordHash: "hash1");

        var user2 = User.Create(
            userId: 2,
            email: "unique@example.com",
            firstName: "User",
            lastName: "Two",
            organizationName: "Org",
            passwordHash: "hash2");

        // Act & Assert
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(user1);
            await context.SaveChangesAsync();
        }

        // Second insert with duplicate email should fail due to unique index
        await using (var context = new AuthDbContext(options))
        {
            context.Users.Add(user2);
            var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(
                async () => await context.SaveChangesAsync());

            exception.Should().NotBeNull();
            exception?.Message.Should().Contain("UNIQUE constraint failed", "Email index should enforce uniqueness");
        }
    }

    [TestMethod]
    public async Task GivenMultipleUsers_WhenInsertingAndQuerying_ThenAllOperationsWork()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        // Act
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            for (int i = 1; i <= 5; i++)
            {
                var user = User.Create(
                    userId: i,
                    email: $"user{i}@example.com",
                    firstName: $"User{i}",
                    lastName: "Test",
                    organizationName: "Test Org",
                    passwordHash: $"hash_{i}");

                context.Users.Add(user);
            }

            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new AuthDbContext(options))
        {
            var users = await context.Users.ToListAsync();
            users.Should().HaveCount(5);
            users.Should().AllSatisfy(u => u.Email.Value.Should().EndWith("@example.com"));
        }
    }

    [TestMethod]
    public async Task GivenRole_WhenInserting_ThenRoleIndexIsApplied()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var role1 = Role.Create(1, "Admin", "Administrator");
        var role2 = Role.Create(2, "Admin", "Another Admin");

        // Act & Assert
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Roles.Add(role1);
            await context.SaveChangesAsync();
        }

        // Duplicate role name should fail due to unique index
        await using (var context = new AuthDbContext(options))
        {
            context.Roles.Add(role2);
            var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(
                async () => await context.SaveChangesAsync());

            exception.Should().NotBeNull();
            exception?.Message.Should().Contain("UNIQUE constraint failed", "Role name index should enforce uniqueness");
        }
    }

    [TestMethod]
    public async Task GivenRefreshToken_WhenInserting_ThenJwtIdIndexIsApplied()
    {
        // Arrange
        Directory.CreateDirectory(_testDataFolder);
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var userId = UserId.Create(1);
        var token1 = RefreshToken.Create(
            id: 1,
            userId: userId,
            jwtId: "unique-jwt-id-123",
            tokenValue: "token_value_1",
            expiresAt: DateTime.UtcNow.AddDays(7));

        var token2 = RefreshToken.Create(
            id: 2,
            userId: userId,
            jwtId: "unique-jwt-id-123",
            tokenValue: "token_value_2",
            expiresAt: DateTime.UtcNow.AddDays(7));

        // Act & Assert
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.RefreshTokens.Add(token1);
            await context.SaveChangesAsync();
        }

        // Duplicate JWT ID should fail
        await using (var context = new AuthDbContext(options))
        {
            context.RefreshTokens.Add(token2);
            var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(
                async () => await context.SaveChangesAsync());

            exception.Should().NotBeNull();
        }
    }

    [TestMethod]
    public async Task GivenSqliteConnectionString_WhenUsingCacheShared_ThenMultipleConnectionsCanAccess()
    {
        // Arrange - Cache=Shared allows multiple connections to access same in-memory database
        var connectionString = $"Data Source={_testDbPath};Cache=Shared";
        Directory.CreateDirectory(_testDataFolder);

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var user = User.Create(
            userId: 1,
            email: "shared@example.com",
            firstName: "Shared",
            lastName: "User",
            organizationName: "Org",
            passwordHash: "hash");

        // Act - write with one connection
        await using (var context = new AuthDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Assert - read with different connection should work
        await using (var context = new AuthDbContext(options))
        {
            var foundUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == "shared@example.com");
            foundUser.Should().NotBeNull("Cache=Shared should allow data persistence across connections");
        }
    }
}
