using Microsoft.EntityFrameworkCore;
using UnitConverter.UserManagement.Application.Services;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;
using UnitConverter.Common.Security;

namespace UnitConverter.UserManagement.DataAccess.Data;

/// <summary>
/// MVP seed: roles and test users (password for all: <c>Test@12345</c>).
/// </summary>
internal static class UserManagementSeedData
{
    public const string DefaultPassword = "Test@12345";

    public static async Task SeedAsync(AuthDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(db, cancellationToken);
        await SeedUsersAsync(db, cancellationToken);
    }

    private static async Task SeedRolesAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        foreach (var name in RoleNames.All)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == name, cancellationToken))
            {
                db.Roles.Add(Role.Create(RoleId(name), name, $"{name} role"));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedUsersAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        var hasher = new PasswordService();
        var hash = hasher.HashPassword(DefaultPassword);

        await EnsureUserAsync(db, hash, 1, "admin@unitconverter.local", "Admin", "User", RoleNames.Admin, cancellationToken);
        await EnsureUserAsync(db, hash, 2, "employee@unitconverter.local", "Employee", "User", RoleNames.Employee, cancellationToken);
        await EnsureUserAsync(db, hash, 3, "partner@unitconverter.local", "Partner", "User", RoleNames.Partner, cancellationToken);
        await EnsureUserAsync(db, hash, 4, "public@unitconverter.local", "Public", "User", RoleNames.Public, cancellationToken);
    }

    private static async Task EnsureUserAsync(
        AuthDbContext db,
        string passwordHash,
        long id,
        string email,
        string firstName,
        string lastName,
        string roleName,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.Id == UserId.Create(id), cancellationToken))
        {
            return;
        }

        var role = await db.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
        var user = User.Create(id, email, firstName, lastName, "UnitConverter", passwordHash);
        user.AssignRole(role);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static int RoleId(string roleName) =>
        roleName switch
        {
            RoleNames.Admin => 1,
            RoleNames.Partner => 2,
            RoleNames.Employee => 3,
            RoleNames.Public => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(roleName), roleName, null)
        };
}
