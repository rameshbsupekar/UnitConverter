using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the Auth Service.
/// Manages entities: User, Role, RefreshToken, TokenBlacklist.
/// Supports SQL Server and SQLite (for testing).
/// </summary>
public class AuthDbContext : DbContext
{
    /// <summary>
    /// DbSet for User entities.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// DbSet for Role entities.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// DbSet for RefreshToken entities.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// DbSet for TokenBlacklist entries.
    /// </summary>
    public DbSet<TokenBlacklist> TokenBlacklists { get; set; } = null!;

    /// <summary>
    /// Constructor for dependency injection (ASP.NET Core).
    /// </summary>
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the schema, relationships, indexes, and value converters.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureRoleEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);
        ConfigureTokenBlacklistEntity(modelBuilder);
        ConfigureRelationships(modelBuilder);
    }

    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        var userBuilder = modelBuilder.Entity<User>();

        userBuilder.HasKey(u => u.Id);

        userBuilder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .ValueGeneratedNever();

        userBuilder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasMaxLength(254)
            .IsRequired();

        userBuilder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        userBuilder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        userBuilder.Property(u => u.OrganizationName)
            .HasMaxLength(255)
            .IsRequired();

        userBuilder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        userBuilder.Property(u => u.CreatedAt)
            .HasPrecision(6)
            .IsRequired();

        userBuilder.Property(u => u.UpdatedAt)
            .HasPrecision(6)
            .IsRequired();

        userBuilder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        userBuilder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_Unique");

        userBuilder.Ignore(u => u.Roles.Count);
    }

    private static void ConfigureRoleEntity(ModelBuilder modelBuilder)
    {
        var roleBuilder = modelBuilder.Entity<Role>();

        roleBuilder.HasKey(r => r.Id);

        roleBuilder.Property(r => r.Id)
            .ValueGeneratedNever();

        roleBuilder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        roleBuilder.Property(r => r.Description)
            .HasMaxLength(500);

        roleBuilder.Property(r => r.CreatedAt)
            .HasPrecision(6)
            .IsRequired();

        roleBuilder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name_Unique");
    }

    private static void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        var tokenBuilder = modelBuilder.Entity<RefreshToken>();

        tokenBuilder.HasKey(t => t.Id);

        tokenBuilder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        tokenBuilder.Property(t => t.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        tokenBuilder.Property(t => t.JwtId)
            .HasMaxLength(255)
            .IsRequired();

        tokenBuilder.Property(t => t.TokenValue)
            .HasMaxLength(1000)
            .IsRequired();

        tokenBuilder.Property(t => t.ExpiresAt)
            .HasPrecision(6)
            .IsRequired();

        tokenBuilder.Property(t => t.CreatedAt)
            .HasPrecision(6)
            .IsRequired();

        tokenBuilder.Property(t => t.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        tokenBuilder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        tokenBuilder.HasIndex(t => t.JwtId)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_JwtId_Unique");
    }

    private static void ConfigureTokenBlacklistEntity(ModelBuilder modelBuilder)
    {
        var blacklistBuilder = modelBuilder.Entity<TokenBlacklist>();

        blacklistBuilder.HasKey(b => b.Id);

        blacklistBuilder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        blacklistBuilder.Property(b => b.JwtIdHash)
            .HasMaxLength(255)
            .IsRequired();

        blacklistBuilder.Property(b => b.BlacklistedAt)
            .HasPrecision(6)
            .IsRequired();

        blacklistBuilder.Property(b => b.TokenExpiresAt)
            .HasPrecision(6)
            .IsRequired();

        blacklistBuilder.HasIndex(b => b.JwtIdHash)
            .IsUnique()
            .HasDatabaseName("IX_TokenBlacklists_JwtIdHash_Unique");

        blacklistBuilder.HasIndex(b => b.TokenExpiresAt)
            .HasDatabaseName("IX_TokenBlacklists_TokenExpiresAt");
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        var userRoleBuilder = modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity(
                "UserRoles",
                l => l.HasOne(typeof(Role)).WithMany().HasForeignKey("RoleId"),
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UserId"),
                je =>
                {
                    je.HasKey("UserId", "RoleId");
                    je.ToTable("UserRoles");
                    je.HasIndex("RoleId").HasDatabaseName("IX_UserRoles_RoleId");
                });

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
