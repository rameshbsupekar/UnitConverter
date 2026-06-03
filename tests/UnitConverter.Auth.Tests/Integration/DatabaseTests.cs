using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Tests.Integration;

/// <summary>
/// Integration tests for database operations.
/// Uses in-memory SQLite for fast, isolated testing without network I/O.
/// Organized with BDD-style test names for clarity.
/// </summary>
[TestClass]
public class DatabaseTests
{
    private DbContextOptions<AuthDbContext> CreateInMemoryDbOptions()
    {
        return new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
    }

    private AuthDbContext CreateContext()
    {
        var options = CreateInMemoryDbOptions();
        var context = new AuthDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [TestMethod]
    public async Task GivenEmptyDatabase_WhenInsertingUser_ThenUserIsPersisted()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Users.Should().HaveCount(1);
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Value.Should().Be("john@example.com");
    }

    [TestMethod]
    public async Task GivenUser_WhenUpdatingEmail_ThenChangesArePersisted()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.UpdateEmail("newemail@example.com");
        await context.SaveChangesAsync();

        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.Email.Value.Should().Be("newemail@example.com");
    }

    [TestMethod]
    public async Task GivenUser_WhenDeactivating_ThenIsActiveIsFalse()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.Deactivate();
        await context.SaveChangesAsync();

        var deactivatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        deactivatedUser!.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public async Task GivenMultipleUsers_WhenQuerying_ThenAllUsersAreReturned()
    {
        using var context = CreateContext();

        for (int i = 1; i <= 5; i++)
        {
            var user = User.Create(
                userId: i,
                email: $"user{i}@example.com",
                firstName: $"User{i}",
                lastName: "Test",
                organizationName: "ACME Corp",
                passwordHash: "hashed_password_123");
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        var users = await context.Users.ToListAsync();
        users.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task GivenUserByEmail_WhenSearching_ThenUserIsFound()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var email = Email.Create("john@example.com");
        var foundUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        foundUser.Should().NotBeNull();
        foundUser!.FirstName.Should().Be("John");
    }

    [TestMethod]
    public async Task GivenUserWithoutRoles_WhenAssigningRole_ThenRoleIsAssociated()
    {
        using var context = CreateContext();

        var role = Role.Create(1, "Admin", "Administrator role");
        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.AssignRole(role);
        await context.SaveChangesAsync();

        var userWithRoles = await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        userWithRoles!.Roles.Should().HaveCount(1);
        userWithRoles.Roles.First().Name.Should().Be("Admin");
    }

    [TestMethod]
    public async Task GivenRoles_WhenInserting_ThenRolesArePersisted()
    {
        using var context = CreateContext();

        var adminRole = Role.Create(1, "Admin", "Administrator");
        var userRole = Role.Create(2, "User", "Regular user");
        var partnerRole = Role.Create(3, "Partner", "Partner user");

        context.Roles.AddRange(adminRole, userRole, partnerRole);
        await context.SaveChangesAsync();

        var roles = await context.Roles.ToListAsync();
        roles.Should().HaveCount(3);
        roles.Any(r => r.Name == "Admin").Should().BeTrue();
    }

    [TestMethod]
    public async Task GivenMultipleRoles_WhenSearchingByName_ThenCorrectRoleIsFound()
    {
        using var context = CreateContext();

        var adminRole = Role.Create(1, "Admin", "Administrator");
        var userRole = Role.Create(2, "User", "Regular user");

        context.Roles.AddRange(adminRole, userRole);
        await context.SaveChangesAsync();

        var foundRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        foundRole.Should().NotBeNull();
        foundRole!.Id.Should().Be(1);
    }

    [TestMethod]
    public async Task GivenRefreshToken_WhenInserting_ThenTokenIsPersisted()
    {
        using var context = CreateContext();

        var userId = UserId.Create(1);
        var token = RefreshToken.Create(
            id: 1,
            userId: userId,
            jwtId: "jwt-id-123",
            tokenValue: "refresh-token-value",
            expiresAt: DateTime.UtcNow.AddDays(7));

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var savedToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
        savedToken.Should().NotBeNull();
        savedToken!.JwtId.Should().Be("jwt-id-123");
        savedToken.IsRevoked.Should().BeFalse();
    }

    [TestMethod]
    public async Task GivenRefreshToken_WhenRevoking_ThenIsRevokedIsTrue()
    {
        using var context = CreateContext();

        var userId = UserId.Create(1);
        var token = RefreshToken.Create(
            id: 1,
            userId: userId,
            jwtId: "jwt-id-123",
            tokenValue: "refresh-token-value",
            expiresAt: DateTime.UtcNow.AddDays(7));

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        token.Revoke();
        await context.SaveChangesAsync();

        var revokedToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
        revokedToken!.IsRevoked.Should().BeTrue();
    }

    [TestMethod]
    public async Task GivenTokenBlacklist_WhenInserting_ThenEntryIsPersisted()
    {
        using var context = CreateContext();

        var entry = TokenBlacklist.Create(
            id: 1,
            jwtIdHash: "hashed-jwt-id-123",
            tokenExpiresAt: DateTime.UtcNow.AddDays(7));

        context.TokenBlacklists.Add(entry);
        await context.SaveChangesAsync();

        var savedEntry = await context.TokenBlacklists.FirstOrDefaultAsync(t => t.Id == entry.Id);
        savedEntry.Should().NotBeNull();
        savedEntry!.JwtIdHash.Should().Be("hashed-jwt-id-123");
    }

    [TestMethod]
    public async Task GivenUser_WhenDeletingUser_ThenUserIsRemoved()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().BeNull();
    }

    [TestMethod]
    public async Task GivenUser_WhenUpdatingPasswordHash_ThenChangeIsPersisted()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "old_hash");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.UpdatePasswordHash("new_hash");
        await context.SaveChangesAsync();

        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.PasswordHash.Should().Be("new_hash");
    }

    [TestMethod]
    public async Task GivenUserWithMultipleRoles_WhenRemovingRole_ThenRoleIsRemoved()
    {
        using var context = CreateContext();

        var adminRole = Role.Create(1, "Admin", "Administrator");
        var userRole = Role.Create(2, "User", "Regular user");
        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Roles.AddRange(adminRole, userRole);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.AssignRole(adminRole);
        user.AssignRole(userRole);
        await context.SaveChangesAsync();

        user.Roles.Should().HaveCount(2);

        user.RemoveRole(adminRole);
        await context.SaveChangesAsync();

        var updatedUser = await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        updatedUser!.Roles.Should().HaveCount(1);
        updatedUser.Roles.First().Name.Should().Be("User");
    }

    [TestMethod]
    public async Task GivenDuplicateEmail_WhenInsertingSecondUser_ThenExceptionIsThrown()
    {
        using var context = CreateContext();

        var user1 = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        var user2 = User.Create(
            userId: 2,
            email: "john@example.com",
            firstName: "Jane",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_456");

        context.Users.Add(user1);
        await context.SaveChangesAsync();

        context.Users.Add(user2);
        
        var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GivenUser_WhenReactivating_ThenIsActiveIsTrue()
    {
        using var context = CreateContext();

        var user = User.Create(
            userId: 1,
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp",
            passwordHash: "hashed_password_123");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.Deactivate();
        await context.SaveChangesAsync();
        
        user.Activate();
        await context.SaveChangesAsync();

        var reactivatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        reactivatedUser!.IsActive.Should().BeTrue();
    }
}
