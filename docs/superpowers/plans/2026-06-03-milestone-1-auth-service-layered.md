# Milestone 1: Auth Service (Layered Architecture) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended). This plan is designed for fresh subagents per task with TDD + two-stage review.

**Goal:** Build the Auth Service microservice with clean layered architecture (Core → Common → Business Logic → API), full security, JWT tokens, role-based authorization, API key management for partners.

**Architecture:** 
- **Layer 1 (Core):** Domain models, entities, value objects, domain events
- **Layer 2 (Common):** Shared abstractions (IRepository, IUnitOfWork, ILogger, IDataProtector)
- **Layer 3 (Business Logic):** Use cases, handlers, validation, business rules
- **Layer 4 (API):** Controllers, DTOs, dependency injection, middleware

**Tech Stack:** 
- ASP.NET Core 8, Entity Framework Core, SQL Server/SQLite
- JWT (System.IdentityModel.Tokens.Jwt), ASP.NET Core Identity (optional enhancement)
- Moq for mocking, MSTest for unit tests
- Security: HTTPS, HSTS, rate limiting, audit logging

---

## File Structure

### Core Layer (Domain)
```
src/UnitConverter.Auth/
├── Core/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs                    # Core user aggregate
│   │   │   ├── Role.cs                    # Role entity
│   │   │   ├── RefreshToken.cs            # Refresh token entity
│   │   │   └── TokenBlacklist.cs          # Revoked tokens
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs                   # Email value object (validation)
│   │   │   ├── Password.cs                # Password value object (hashing)
│   │   │   └── UserId.cs                  # Strongly-typed ID
│   │   ├── Events/
│   │   │   ├── UserRegisteredEvent.cs
│   │   │   ├── UserLoggedInEvent.cs
│   │   │   └── TokenRevokedEvent.cs
│   │   └── Exceptions/
│   │       ├── InvalidEmailException.cs
│   │       ├── WeakPasswordException.cs
│   │       ├── UserAlreadyExistsException.cs
│   │       └── InvalidCredentialsException.cs
```

### Common Layer (Shared Abstractions)
```
src/UnitConverter.Auth/
├── Common/
│   ├── Interfaces/
│   │   ├── IRepository.cs                 # Generic repository interface
│   │   ├── IUserRepository.cs             # User-specific repository
│   │   ├── IRoleRepository.cs             # Role repository
│   │   ├── IUnitOfWork.cs                 # Unit of work pattern
│   │   ├── ITokenGenerator.cs             # JWT token generation
│   │   ├── IPasswordHasher.cs             # Secure password hashing
│   │   ├── IDataProtector.cs              # Sensitive data protection
│   │   ├── IEmailService.cs               # (Future) email notifications
│   │   └── IAuthenticationService.cs      # Auth orchestration
│   ├── Models/
│   │   ├── JwtSettings.cs                 # JWT configuration
│   │   └── ApiKeySettings.cs              # API key configuration
│   └── Constants/
│       └── SecurityConstants.cs           # Password requirements, etc.
```

### Business Logic Layer (Use Cases)
```
src/UnitConverter.Auth/
├── Application/
│   ├── Commands/
│   │   ├── RegisterUserCommand.cs         # Register new user
│   │   ├── LoginCommand.cs                # User login
│   │   ├── RefreshTokenCommand.cs         # Refresh JWT token
│   │   ├── RevokeTokenCommand.cs          # Logout
│   │   ├── GenerateApiKeyCommand.cs       # Partner API key
│   │   └── RevokeApiKeyCommand.cs         # Revoke partner key
│   ├── Handlers/
│   │   ├── RegisterUserCommandHandler.cs
│   │   ├── LoginCommandHandler.cs
│   │   ├── RefreshTokenCommandHandler.cs
│   │   ├── RevokeTokenCommandHandler.cs
│   │   ├── GenerateApiKeyCommandHandler.cs
│   │   └── RevokeApiKeyCommandHandler.cs
│   ├── Queries/
│   │   ├── ValidateTokenQuery.cs          # Token validation (internal)
│   │   ├── GetUserByIdQuery.cs            # Get user details
│   │   └── GetUserRolesQuery.cs           # Get user roles
│   ├── QueryHandlers/
│   │   ├── ValidateTokenQueryHandler.cs
│   │   ├── GetUserByIdQueryHandler.cs
│   │   └── GetUserRolesQueryHandler.cs
│   ├── DTOs/
│   │   ├── RegisterUserRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── TokenResponse.cs
│   │   ├── UserResponse.cs
│   │   └── ApiKeyResponse.cs
│   ├── Validation/
│   │   ├── RegisterUserValidator.cs       # Fluent validation rules
│   │   ├── LoginValidator.cs
│   │   └── RefreshTokenValidator.cs
│   └── Services/
│       ├── JwtTokenService.cs             # Token generation & validation
│       ├── PasswordService.cs             # Password hashing (bcrypt)
│       └── ApiKeyService.cs               # API key management
```

### API Layer (Controllers & Dependency Injection)
```
src/UnitConverter.Auth/
├── API/
│   ├── Controllers/
│   │   ├── AuthController.cs              # Auth endpoints
│   │   └── InternalController.cs          # Internal token validation
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── AuditLoggingMiddleware.cs
│   │   ├── SecurityHeadersMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs # DI registration
│   │   └── MiddlewareExtensions.cs        # Middleware setup
│   └── Program.cs                         # Startup configuration
```

### Tests
```
tests/UnitConverter.Auth.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── UserTests.cs
│   │   ├── EmailTests.cs
│   │   └── PasswordTests.cs
│   ├── Application/
│   │   ├── RegisterUserHandlerTests.cs
│   │   ├── LoginHandlerTests.cs
│   │   ├── TokenGenerationTests.cs
│   │   └── ApiKeyServiceTests.cs
│   └── Validation/
│       └── PasswordValidationTests.cs
├── Integration/
│   ├── AuthControllerTests.cs
│   └── TokenEndpointTests.cs
└── Fixtures/
    ├── TestUserFactory.cs
    └── JwtTestHelper.cs
```

---

## Dependency Flow (Inbound Only)

```
API Layer (Controllers)
    ↓ (depends on)
Business Logic Layer (Handlers, Services)
    ↓ (depends on)
Common Layer (Interfaces)
    ↓ (depends on)
Core Layer (Domain Models, Entities)

✓ NO UPWARD DEPENDENCIES
✓ Core has NO dependencies (except System)
✓ Common defines abstractions (Interfaces)
✓ Business Logic implements interfaces
✓ API layer orchestrates via dependency injection
```

---

## Tasks (Bite-Sized, TDD, Frequent Commits)

### Task 1: Create Core Domain Models (User, Role, ValueObjects)

**Files:**
- Create: `src/UnitConverter.Auth/Core/Domain/ValueObjects/UserId.cs`
- Create: `src/UnitConverter.Auth/Core/Domain/ValueObjects/Email.cs`
- Create: `src/UnitConverter.Auth/Core/Domain/Entities/User.cs`
- Create: `src/UnitConverter.Auth/Core/Domain/Entities/Role.cs`
- Create: `src/UnitConverter.Auth/Core/Domain/Exceptions/InvalidEmailException.cs`
- Test: `tests/UnitConverter.Auth.Tests/Unit/Domain/EmailTests.cs`
- Test: `tests/UnitConverter.Auth.Tests/Unit/Domain/UserTests.cs`

#### Step 1: Write failing test for Email value object
```bash
File: tests/UnitConverter.Auth.Tests/Unit/Domain/EmailTests.cs
```

```csharp
[TestClass]
public class EmailTests
{
    [TestMethod]
    public void Create_ValidEmail_Succeeds()
    {
        var email = Email.Create("user@example.com");
        Assert.AreEqual("user@example.com", email.Value);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidEmailException))]
    public void Create_InvalidEmail_ThrowsException()
    {
        Email.Create("not-an-email");
    }

    [TestMethod]
    public void Equality_SameEmail_AreEqual()
    {
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("user@example.com");
        Assert.AreEqual(email1, email2);
    }
}
```

Run: `dotnet test tests/UnitConverter.Auth.Tests/Unit/Domain/EmailTests.cs`  
Expected: FAIL - `Email type not found`

#### Step 2: Create Email value object
```bash
File: src/UnitConverter.Auth/Core/Domain/ValueObjects/Email.cs
```

```csharp
using System;
using System.Text.RegularExpressions;
using UnitConverter.Auth.Core.Domain.Exceptions;

namespace UnitConverter.Auth.Core.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidEmailException("Email cannot be empty");

        var trimmed = value.Trim().ToLowerInvariant();

        if (trimmed.Length > 254)
            throw new InvalidEmailException("Email is too long");

        if (!Regex.IsMatch(trimmed, EmailPattern))
            throw new InvalidEmailException($"Invalid email format: {trimmed}");

        return new Email(trimmed);
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public bool Equals(Email? other) =>
        other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
```

#### Step 3: Create InvalidEmailException
```bash
File: src/UnitConverter.Auth/Core/Domain/Exceptions/InvalidEmailException.cs
```

```csharp
namespace UnitConverter.Auth.Core.Domain.Exceptions;

public sealed class InvalidEmailException : DomainException
{
    public InvalidEmailException(string message) : base(message) { }
}

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
```

#### Step 4: Run tests and verify pass
```bash
dotnet test tests/UnitConverter.Auth.Tests/Unit/Domain/EmailTests.cs -v
Expected: PASS
```

#### Step 5: Create UserId value object (similar pattern)
```bash
File: src/UnitConverter.Auth/Core/Domain/ValueObjects/UserId.cs
```

```csharp
namespace UnitConverter.Auth.Core.Domain.ValueObjects;

public sealed class UserId : IEquatable<UserId>
{
    public long Value { get; }

    private UserId(long value)
    {
        if (value <= 0)
            throw new ArgumentException("UserId must be greater than 0");
        
        Value = value;
    }

    public static UserId Create(long value) => new(value);

    public override bool Equals(object? obj) => Equals(obj as UserId);
    public bool Equals(UserId? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
```

#### Step 6: Create User aggregate entity
```bash
File: src/UnitConverter.Auth/Core/Domain/Entities/User.cs
```

```csharp
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Core.Domain.Entities;

public sealed class User
{
    public UserId Id { get; }
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string OrganizationName { get; private set; }
    public string PasswordHash { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private User() { }  // EF Core

    public static User Create(
        UserId id,
        Email email,
        string firstName,
        string lastName,
        string organizationName,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name required");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name required");

        return new User
        {
            Id = id,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            OrganizationName = organizationName,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateEmail(Email newEmail)
    {
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### Step 7: Create Role entity
```bash
File: src/UnitConverter.Auth/Core/Domain/Entities/Role.cs
```

```csharp
namespace UnitConverter.Auth.Core.Domain.Entities;

public sealed class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;  // "Admin", "Partner", "Employee", "Public"
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // For EF relationships
    public List<User> Users { get; set; } = new();
}
```

#### Step 8: Run all domain tests
```bash
dotnet test tests/UnitConverter.Auth.Tests/Unit/Domain/ -v
Expected: All tests PASS
```

#### Step 9: Commit
```bash
git add src/UnitConverter.Auth/Core/ tests/UnitConverter.Auth.Tests/Unit/Domain/
git commit -m "feat: create Auth Service domain models (User, Email, Role, value objects)"
```

---

### Task 2: Create Common Layer (Repository, Token, Password Interfaces)

**Files:**
- Create: `src/UnitConverter.Auth/Common/Interfaces/IRepository.cs`
- Create: `src/UnitConverter.Auth/Common/Interfaces/IUserRepository.cs`
- Create: `src/UnitConverter.Auth/Common/Interfaces/ITokenGenerator.cs`
- Create: `src/UnitConverter.Auth/Common/Interfaces/IPasswordHasher.cs`
- Create: `src/UnitConverter.Auth/Common/Models/JwtSettings.cs`
- Create: `src/UnitConverter.Auth/Common/Constants/SecurityConstants.cs`

#### Step 1: Create generic repository interface
```bash
File: src/UnitConverter.Auth/Common/Interfaces/IRepository.cs
```

```csharp
using System.Linq.Expressions;

namespace UnitConverter.Auth.Common.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task SaveAsync();
}
```

#### Step 2: Create user-specific repository interface
```bash
File: src/UnitConverter.Auth/Common/Interfaces/IUserRepository.cs
```

```csharp
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(Email email);
    Task<bool> EmailExistsAsync(Email email);
    Task<User?> GetWithRolesAsync(UserId userId);
}
```

#### Step 3: Create token generator interface
```bash
File: src/UnitConverter.Auth/Common/Interfaces/ITokenGenerator.cs
```

```csharp
using UnitConverter.Auth.Core.Domain.Entities;

namespace UnitConverter.Auth.Common.Interfaces;

public interface ITokenGenerator
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    TokenValidationResult ValidateToken(string token);
}

public sealed class TokenValidationResult
{
    public bool IsValid { get; set; }
    public long UserId { get; set; }
    public string Email { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}
```

#### Step 4: Create password hasher interface
```bash
File: src/UnitConverter.Auth/Common/Interfaces/IPasswordHasher.cs
```

```csharp
namespace UnitConverter.Auth.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

#### Step 5: Create JWT settings model
```bash
File: src/UnitConverter.Auth/Common/Models/JwtSettings.cs
```

```csharp
namespace UnitConverter.Auth.Common.Models;

public sealed class JwtSettings
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
```

#### Step 6: Create security constants
```bash
File: src/UnitConverter.Auth/Common/Constants/SecurityConstants.cs
```

```csharp
namespace UnitConverter.Auth.Common.Constants;

public static class SecurityConstants
{
    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 128;
    public const int ApiKeyMinLength = 32;
    
    // Password requirements
    public static bool IsValidPassword(string password)
    {
        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            return false;
        
        bool hasUppercase = password.Any(char.IsUpper);
        bool hasLowercase = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
        
        return hasUppercase && hasLowercase && hasDigit && hasSpecialChar;
    }

    public static string PasswordRequirementsMessage =>
        "Password must be 12-128 characters, with at least 1 uppercase, 1 lowercase, 1 digit, 1 special character";

    public const string AdminRole = "Admin";
    public const string PartnerRole = "Partner";
    public const string EmployeeRole = "Employee";
    public const string PublicRole = "Public";
}
```

#### Step 7: Run build to verify no compile errors
```bash
dotnet build src/UnitConverter.Auth/ -c Release
Expected: Build succeeds
```

#### Step 8: Commit
```bash
git add src/UnitConverter.Auth/Common/
git commit -m "feat: create Auth Service common layer (interfaces, models, constants)"
```

---

### Task 3: Create Business Logic Layer (JWT Token Service Implementation)

**Files:**
- Create: `src/UnitConverter.Auth/Application/Services/JwtTokenService.cs`
- Create: `src/UnitConverter.Auth/Application/Services/PasswordService.cs`
- Create: `tests/UnitConverter.Auth.Tests/Unit/Application/TokenGenerationTests.cs`
- Create: `tests/UnitConverter.Auth.Tests/Unit/Application/PasswordServiceTests.cs`

#### Step 1: Write failing test for JWT token generation
```bash
File: tests/UnitConverter.Auth.Tests/Unit/Application/TokenGenerationTests.cs
```

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UnitConverter.Auth.Application.Services;
using UnitConverter.Auth.Common.Models;

[TestClass]
public class TokenGenerationTests
{
    private JwtTokenService _tokenService = null!;
    private JwtSettings _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _settings = new JwtSettings
        {
            Secret = "dev-secret-key-min-32-chars-required-for-hs256",
            Issuer = "http://localhost:5001",
            Audience = "unitconverter-api",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        _tokenService = new JwtTokenService(_settings);
    }

    [TestMethod]
    public void GenerateAccessToken_ValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = UserId.Create(1),
            Email = Email.Create("user@example.com"),
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME"
        };
        var roles = new[] { "Employee" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.IsTrue(token.Split('.').Length == 3);  // JWT format: header.payload.signature
    }

    [TestMethod]
    public void ValidateToken_ValidToken_ReturnsValid()
    {
        // Arrange
        var user = new User
        {
            Id = UserId.Create(1),
            Email = Email.Create("user@example.com"),
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME"
        };
        var roles = new[] { "Employee" };
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Act
        var result = _tokenService.ValidateToken(token);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(1L, result.UserId);
        Assert.AreEqual("user@example.com", result.Email);
        Assert.IsTrue(result.Roles.Contains("Employee"));
    }

    [TestMethod]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Act
        var result = _tokenService.ValidateToken("invalid.token.here");

        // Assert
        Assert.IsFalse(result.IsValid);
    }
}
```

Run: `dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/TokenGenerationTests.cs`  
Expected: FAIL - `JwtTokenService not found`

#### Step 2: Implement JWT token service
```bash
File: src/UnitConverter.Auth/Application/Services/JwtTokenService.cs
```

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Common.Models;
using UnitConverter.Auth.Core.Domain.Entities;

namespace UnitConverter.Auth.Application.Services;

public sealed class JwtTokenService : ITokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
            new Claim(ClaimTypes.Email, user.Email.Value),
            new Claim("org_id", user.OrganizationName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }

    public TokenValidationResult ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = long.Parse(userIdClaim ?? "0"),
                Email = emailClaim ?? string.Empty,
                Roles = roleClaims
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

#### Step 3: Run token tests
```bash
dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/TokenGenerationTests.cs -v
Expected: PASS
```

#### Step 4: Write failing test for password service
```bash
File: tests/UnitConverter.Auth.Tests/Unit/Application/PasswordServiceTests.cs
```

```csharp
using UnitConverter.Auth.Application.Services;
using UnitConverter.Auth.Common.Constants;

[TestClass]
public class PasswordServiceTests
{
    private PasswordService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new PasswordService();
    }

    [TestMethod]
    public void HashPassword_ValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "SecurePassword123!@#";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
        Assert.AreNotEqual(password, hash);
    }

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123!@#";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword123!@#";
        var wrongPassword = "WrongPassword123!@#";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.IsFalse(result);
    }
}
```

Run: `dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/PasswordServiceTests.cs`  
Expected: FAIL - `PasswordService not found`

#### Step 5: Implement password service (bcrypt)
```bash
File: src/UnitConverter.Auth/Application/Services/PasswordService.cs
```

```csharp
using BC = BCrypt.Net.BCrypt;
using UnitConverter.Auth.Common.Interfaces;

namespace UnitConverter.Auth.Application.Services;

public sealed class PasswordService : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        return BC.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BC.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
```

#### Step 6: Add BCrypt NuGet package
```bash
dotnet add src/UnitConverter.Auth/UnitConverter.Auth.csproj package BCrypt.Net-Core
```

#### Step 7: Run all application tests
```bash
dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/ -v
Expected: All tests PASS
```

#### Step 8: Commit
```bash
git add src/UnitConverter.Auth/Application/ tests/UnitConverter.Auth.Tests/Unit/Application/
git commit -m "feat: implement JWT token service and password hashing (bcrypt)"
```

---

### Task 4: Create Business Logic Layer (Command Handlers - Register User)

**Files:**
- Create: `src/UnitConverter.Auth/Application/Commands/RegisterUserCommand.cs`
- Create: `src/UnitConverter.Auth/Application/DTOs/RegisterUserRequest.cs`
- Create: `src/UnitConverter.Auth/Application/Handlers/RegisterUserCommandHandler.cs`
- Create: `src/UnitConverter.Auth/Application/Validation/RegisterUserValidator.cs`
- Create: `tests/UnitConverter.Auth.Tests/Unit/Application/RegisterUserHandlerTests.cs`

#### Step 1: Create command and DTO
```bash
File: src/UnitConverter.Auth/Application/Commands/RegisterUserCommand.cs
```

```csharp
namespace UnitConverter.Auth.Application.Commands;

public sealed class RegisterUserCommand
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string OrganizationName { get; set; } = null!;
}
```

```bash
File: src/UnitConverter.Auth/Application/DTOs/RegisterUserRequest.cs
```

```csharp
namespace UnitConverter.Auth.Application.DTOs;

public sealed class RegisterUserRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string OrganizationName { get; set; } = null!;
}

public sealed class UserResponse
{
    public long UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}
```

#### Step 2: Write failing test for register handler
```bash
File: tests/UnitConverter.Auth.Tests/Unit/Application/RegisterUserHandlerTests.cs
```

```csharp
using Moq;
using UnitConverter.Auth.Application.Commands;
using UnitConverter.Auth.Application.Handlers;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Core.Domain.Exceptions;
using UnitConverter.Auth.Core.Domain.ValueObjects;

[TestClass]
public class RegisterUserHandlerTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPasswordHasher> _mockPasswordHasher = null!;
    private RegisterUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _handler = new RegisterUserCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME"
        };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.UserId > 0);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUserRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(UserAlreadyExistsException))]
    public async Task Handle_EmailAlreadyExists_ThrowsException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "existing@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME"
        };

        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(command);
    }
}
```

Run: `dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/RegisterUserHandlerTests.cs`  
Expected: FAIL - `RegisterUserCommandHandler not found`

#### Step 3: Create validator
```bash
File: src/UnitConverter.Auth/Application/Validation/RegisterUserValidator.cs
```

```csharp
using UnitConverter.Auth.Application.Commands;
using UnitConverter.Auth.Common.Constants;
using FluentValidation;

namespace UnitConverter.Auth.Application.Validation;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(SecurityConstants.PasswordMinLength)
                .WithMessage($"Password must be at least {SecurityConstants.PasswordMinLength} characters")
            .Custom((password, context) =>
            {
                if (!SecurityConstants.IsValidPassword(password))
                {
                    context.AddFailure(SecurityConstants.PasswordRequirementsMessage);
                }
            });

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(1, 100).WithMessage("First name must be 1-100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(1, 100).WithMessage("Last name must be 1-100 characters");

        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Organization name is required")
            .Length(1, 200).WithMessage("Organization name must be 1-200 characters");
    }
}
```

#### Step 4: Add FluentValidation NuGet
```bash
dotnet add src/UnitConverter.Auth/UnitConverter.Auth.csproj package FluentValidation
```

#### Step 5: Implement handler
```bash
File: src/UnitConverter.Auth/Application/Handlers/RegisterUserCommandHandler.cs
```

```csharp
using UnitConverter.Auth.Application.Commands;
using UnitConverter.Auth.Application.DTOs;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.Exceptions;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Application.Handlers;

public sealed class RegisterUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<UserResponse> HandleAsync(RegisterUserCommand command)
    {
        // 1. Validate email
        var email = Email.Create(command.Email);

        // 2. Check if email already exists
        if (await _userRepository.EmailExistsAsync(email))
            throw new UserAlreadyExistsException($"User with email {email.Value} already exists");

        // 3. Hash password
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        // 4. Create user
        var userId = UserId.Create(GenerateNewUserId());  // Simple ID generation for now
        var user = User.Create(
            userId,
            email,
            command.FirstName,
            command.LastName,
            command.OrganizationName,
            passwordHash
        );

        // 5. Assign default role (Public or Employee based on org)
        // Note: Roles fetched from DB in real implementation

        // 6. Save user
        await _userRepository.AddAsync(user);
        await _userRepository.SaveAsync();

        // 7. Return response
        return new UserResponse
        {
            UserId = user.Id.Value,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    private long GenerateNewUserId()
    {
        // Simple implementation; use database sequence or GUID in production
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string message) : base(message) { }
}
```

#### Step 6: Run handler tests
```bash
dotnet test tests/UnitConverter.Auth.Tests/Unit/Application/RegisterUserHandlerTests.cs -v
Expected: PASS
```

#### Step 7: Commit
```bash
git add src/UnitConverter.Auth/Application/Commands/ src/UnitConverter.Auth/Application/Handlers/ src/UnitConverter.Auth/Application/Validation/ src/UnitConverter.Auth/Application/DTOs/ tests/UnitConverter.Auth.Tests/Unit/Application/RegisterUserHandlerTests.cs
git commit -m "feat: implement user registration command handler with validation"
```

---

### Task 5: Create Database & Repository Implementation (EF Core)

**Files:**
- Create: `src/UnitConverter.Auth/Infrastructure/Data/AuthDbContext.cs`
- Create: `src/UnitConverter.Auth/Infrastructure/Repositories/UserRepository.cs`
- Create: `src/UnitConverter.Auth/Infrastructure/Repositories/RoleRepository.cs`
- Create: migrations folder structure
- Test: Integration test (save and retrieve user)

[Continue with remaining tasks in similar detail...]

---

## Summary

**Total Tasks: 12**
- Task 1: Core domain models
- Task 2: Common layer interfaces
- Task 3: Business logic services (JWT, Password)
- Task 4: Register user command handler
- Task 5: Database & repositories
- Task 6: Login command handler
- Task 7: Refresh token handler
- Task 8: API controllers
- Task 9: Dependency injection setup
- Task 10: Security middleware
- Task 11: Integration tests
- Task 12: Docker & deployment readiness

**Architecture Guarantee:**
- ✅ Core layer: NO dependencies (domain models only)
- ✅ Common layer: NO implementation (interfaces only)
- ✅ Business logic: Implements interfaces, depends on Common
- ✅ API: Orchestrates via DI, depends on Business Logic
- ✅ Each layer tests independently (Moq for external dependencies)
- ✅ Interface-based, easily mockable, fully testable

---

## Next Steps

1. Execute this plan task-by-task with TDD
2. After Auth Service completes → Proceed to Milestone 2 (Catalog Service)
3. After Catalog Service → Proceed to Milestone 3 (Conversion Service)
4. After all services → Docker Compose & integration testing

