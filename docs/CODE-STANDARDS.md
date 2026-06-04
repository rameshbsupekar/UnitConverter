# Code Standards & Best Practices

This document establishes coding standards for UnitConverter project following Microsoft Framework Design Guidelines and ASP.NET Core best practices.

---

## 1. Folder Structure & Naming Standards

### Source Code Organization (All in `src/`)

```
src/UnitConverter.Auth/
├── Core/Domain/               # Business logic, entities, domain models (NO dependencies)
│   ├── Entities/              # DDD aggregates, entities
│   ├── ValueObjects/          # Immutable value types
│   ├── Events/                # Domain events
│   └── Exceptions/            # Domain exceptions only
├── Common/                    # Shared abstractions (interfaces only, no impl)
│   ├── Interfaces/            # IRepository, IService contracts
│   ├── Models/                # DTOs, configuration models
│   └── Constants/             # Security, business constants
├── Application/               # Use cases, handlers (implements Common interfaces)
│   ├── Commands/              # Command DTOs
│   ├── Handlers/              # Command/Query handlers
│   ├── Queries/               # Query DTOs
│   ├── Services/              # Application services (implements IService)
│   ├── Validators/            # FluentValidation rules
│   └── DTOs/                  # Request/response models
├── Infrastructure/            # EF Core, repositories, external services
│   ├── Data/                  # DbContext, migrations
│   ├── Repositories/          # Repository implementations (implements IRepository)
│   └── Services/              # External service integrations
├── API/                       # ASP.NET Core controllers, middleware
│   ├── Controllers/           # API endpoints
│   ├── Middleware/            # Custom middleware (exception, logging, etc)
│   ├── Extensions/            # ServiceCollectionExtensions, MiddlewareExtensions
│   └── Program.cs             # Startup configuration
└── UnitConverter.Auth.csproj  # Project file

tests/UnitConverter.Auth.Tests/
├── Unit/                      # Unit tests (no external dependencies, use Moq)
│   ├── Domain/                # Domain model tests
│   ├── Application/           # Handler & service tests
│   └── Common/                # Constant & interface tests
├── Integration/               # Integration tests (with in-memory DB)
│   ├── Repositories/          # Repository tests with EF Core
│   └── Handlers/              # Handler tests with real DB context
├── API/                       # API integration tests (WebApplicationFactory)
│   └── AuthControllerTests.cs
├── Fixtures/                  # Test data factories, helpers
│   ├── TestUserFactory.cs
│   └── TestDataBuilder.cs
└── UnitConverter.Auth.Tests.csproj
```

### Naming Conventions (Microsoft Framework Design Guidelines)

| Item | Convention | Example |
|------|-----------|---------|
| **Namespaces** | PascalCase, hierarchical | `UnitConverter.UserManagement.Application.Commands` |
| **Classes** | PascalCase | `User`, `UserRepository`, `RegisterUserRequest` |
| **Interfaces** | I + PascalCase | `IUserRepository`, `ITokenGenerator` |
| **Methods** | PascalCase, verb + noun | `GetUserById`, `ValidateToken`, `CreateAsync` |
| **Properties** | PascalCase | `Email`, `FirstName`, `CreatedAt` |
| **Local variables** | camelCase | `userId`, `userEmail`, `isValid` |
| **Constants** | UPPER_SNAKE_CASE (or PascalCase if well-known) | `MAX_PASSWORD_LENGTH`, `AdminRole` |
| **Async methods** | Verb + Async | `GetUserByIdAsync`, `SaveAsync`, `ValidateAsync` |
| **Booleans** | Is/Has/Can prefix | `IsActive`, `HasRole`, `CanApprove` |
| **Test methods** | Method_Scenario_Expected | `CreateUser_ValidEmail_ReturnsUserId` |
| **Test classes** | ClassName + Tests | `UserTests`, `TokenGenerationTests`, `RepositoryTests` |

---

## 2. Unit Test Standards (TDD + BDD Best Practices)

### Test Organization (AAA Pattern)

```csharp
[TestClass]
public class UserTests
{
    [TestMethod]
    public void Create_ValidInput_ReturnsUser()
    {
        // Arrange: Set up test data and dependencies
        var email = Email.Create("user@example.com");
        var userId = UserId.Create(1);

        // Act: Execute the method under test
        var user = User.Create(
            userId, email, "John", "Doe", "ACME", "hash"
        );

        // Assert: Verify expected behavior
        Assert.IsNotNull(user);
        Assert.AreEqual(email, user.Email);
    }
}
```

### BDD-Based Test Grouping (By Scenario, Not Implementation)

**DO:** Group tests by user story/behavior, not by technical layer

```csharp
[TestClass]
public class UserRegistrationTests  // Feature: User registration
{
    // Scenario 1: Happy path - valid registration
    [TestMethod]
    public void Register_ValidInput_CreatesUserSuccessfully() { }

    [TestMethod]
    public void Register_ValidInput_AssignsDefaultRole() { }

    // Scenario 2: Email validation
    [TestMethod]
    public void Register_InvalidEmail_ThrowsException() { }

    [TestMethod]
    public void Register_DuplicateEmail_ThrowsException() { }

    // Scenario 3: Password validation
    [TestMethod]
    public void Register_WeakPassword_ThrowsException() { }

    [TestMethod]
    public void Register_ValidPassword_HashesSuccessfully() { }

    // Scenario 4: Edge cases
    [TestMethod]
    public void Register_LongName_Succeeds() { }

    [TestMethod]
    public void Register_EmptyName_ThrowsException() { }
}
```

**DON'T:** Redundant tests that repeat the same logic

```csharp
// ❌ AVOID: Repetitive tests
[TestMethod]
public void Email_CreateWithValidEmail1_Succeeds() { }

[TestMethod]
public void Email_CreateWithValidEmail2_Succeeds() { }

[TestMethod]
public void Email_CreateWithValidEmail3_Succeeds() { }
```

### Data-Driven Tests (Eliminate Repetition)

```csharp
[TestClass]
public class EmailTests
{
    // ✅ GOOD: Single test, multiple data points
    [DataTestMethod]
    [DataRow("valid@example.com")]
    [DataRow("user+tag@domain.co.uk")]
    [DataRow("123@example.net")]
    public void Create_ValidEmails_Succeed(string email)
    {
        var result = Email.Create(email);
        Assert.IsNotNull(result);
    }

    // ✅ GOOD: Data-driven negative scenarios
    [DataTestMethod]
    [DataRow("invalid.email")]
    [DataRow("@example.com")]
    [DataRow("user@")]
    [DataRow("")]
    [ExpectedException(typeof(InvalidEmailException))]
    public void Create_InvalidEmails_ThrowException(string email)
    {
        Email.Create(email);
    }

    // ✅ GOOD: Multiple assertions per data point
    [DataTestMethod]
    [DataRow("user@example.com", "user@example.com")] // Case normalization
    [DataRow("User@EXAMPLE.COM", "user@example.com")]
    public void Create_NormalizesEmailCase(string input, string expected)
    {
        var email = Email.Create(input);
        Assert.AreEqual(expected, email.Value);
    }
}
```

### Positive & Negative Scenarios (BDD Coverage)

```csharp
[TestClass]
public class TokenGenerationTests
{
    private JwtTokenService _service;

    [TestInitialize]
    public void Setup()
    {
        var settings = new JwtSettings
        {
            Secret = "dev-secret-key-min-32-chars-required",
            Issuer = "http://localhost",
            Audience = "api",
            AccessTokenExpiryMinutes = 15
        };
        _service = new JwtTokenService(settings);
    }

    // ===== POSITIVE SCENARIOS =====

    [TestMethod]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var roles = new[] { "Employee" };

        // Act
        var token = _service.GenerateAccessToken(user, roles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.AreEqual(3, token.Split('.').Length); // JWT format
    }

    [TestMethod]
    public void GenerateAccessToken_IncludesAllClaims_Success()
    {
        var user = TestUserFactory.CreateValidUser();
        var token = _service.GenerateAccessToken(user, new[] { "Admin" });
        
        var handler = new JwtSecurityTokenHandler();
        var claims = handler.ReadJwtToken(token).Claims;

        Assert.IsTrue(claims.Any(c => c.Type == ClaimTypes.NameIdentifier));
        Assert.IsTrue(claims.Any(c => c.Type == ClaimTypes.Email));
        Assert.IsTrue(claims.Any(c => c.Type == ClaimTypes.Role));
    }

    // ===== NEGATIVE SCENARIOS =====

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GenerateAccessToken_NullUser_ThrowsException()
    {
        _service.GenerateAccessToken(null, new[] { "Admin" });
    }

    [TestMethod]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        var result = _service.ValidateToken("invalid.token.here");
        
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_ExpiredToken_ReturnsFalse()
    {
        // Create token with past expiry
        var settings = new JwtSettings
        {
            Secret = "dev-secret-key-min-32-chars-required",
            Issuer = "http://localhost",
            Audience = "api",
            AccessTokenExpiryMinutes = -1 // Already expired
        };
        var service = new JwtTokenService(settings);
        var user = TestUserFactory.CreateValidUser();
        var token = service.GenerateAccessToken(user, new[] { "User" });

        // Validate with normal service
        var result = _service.ValidateToken(token);
        Assert.IsFalse(result.IsValid);
    }

    // ===== EDGE CASES =====

    [TestMethod]
    public void GenerateAccessToken_NoRoles_Succeeds()
    {
        var user = TestUserFactory.CreateValidUser();
        var token = _service.GenerateAccessToken(user, new string[] { });
        
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
    }

    [TestMethod]
    public void GenerateAccessToken_MultipleRoles_IncludesAll()
    {
        var user = TestUserFactory.CreateValidUser();
        var roles = new[] { "Admin", "Partner", "Employee" };
        var token = _service.GenerateAccessToken(user, roles);

        var handler = new JwtSecurityTokenHandler();
        var claims = handler.ReadJwtToken(token).Claims;
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();

        Assert.AreEqual(3, roleClaims.Count);
    }
}
```

### Test Fixtures & Builders (DRY - Don't Repeat Yourself)

```csharp
// ✅ Reusable test data factory
public static class TestUserFactory
{
    public static User CreateValidUser(
        long userId = 1,
        string email = "user@example.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        return User.Create(
            UserId.Create(userId),
            Email.Create(email),
            firstName,
            lastName,
            "ACME Corp",
            "password_hash"
        );
    }

    public static List<User> CreateMultipleUsers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateValidUser(i, $"user{i}@example.com"))
            .ToList();
    }
}

// ✅ Builder pattern for complex objects
public class UserBuilder
{
    private long _userId = 1;
    private string _email = "user@example.com";
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _organizationName = "ACME";
    private string _passwordHash = "hash";

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public User Build()
    {
        return User.Create(
            UserId.Create(_userId),
            Email.Create(_email),
            _firstName,
            _lastName,
            _organizationName,
            _passwordHash
        );
    }
}

// Usage in tests
[TestMethod]
public void SomeTest()
{
    var user = new UserBuilder()
        .WithEmail("custom@example.com")
        .WithFirstName("Jane")
        .Build();
    
    Assert.IsNotNull(user);
}
```

---

## 3. Exception Handling (ASP.NET Core Best Practices)

### Architecture: Global Exception Handling

```csharp
// src/UnitConverter.Auth/API/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, message, details) = exception switch
        {
            // Domain exceptions (400 - Bad Request)
            InvalidEmailException e => 
                (StatusCodes.Status400BadRequest, "Validation failed", e.Message),
            
            WeakPasswordException e => 
                (StatusCodes.Status400BadRequest, "Validation failed", e.Message),
            
            UserAlreadyExistsException e => 
                (StatusCodes.Status409Conflict, "Conflict", e.Message),
            
            // Auth exceptions (401 - Unauthorized)
            InvalidCredentialsException e => 
                (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message),
            
            // Internal server error (500)
            _ => (StatusCodes.Status500InternalServerError, 
                  "Internal server error", 
                  context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true
                      ? exception.Message
                      : "An error occurred")
        };

        context.Response.StatusCode = statusCode;

        var response = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Detail = details,
            Instance = context.Request.Path,
            Type = $"https://api.example.com/errors/{exception.GetType().Name}"
        };

        if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            response.Extensions["stackTrace"] = exception.StackTrace;
            response.Extensions["innerException"] = exception.InnerException?.Message;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### Program.cs Exception Setup

```csharp
// src/UnitConverter.Auth/API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ... Add services ...

var app = builder.Build();

// Development: Show full errors in browser
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapScalarApiReference();
}
else
{
    // Production: Return ProblemDetails JSON
    app.UseExceptionHandler("/error");
}

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

await app.RunAsync();
```

### Error Controller

```csharp
// src/UnitConverter.Auth/API/Controllers/ErrorController.cs
[ApiController]
[Route("error")]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public IActionResult Error()
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception == null)
        {
            return Problem(
                detail: "An unknown error occurred",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        _logger.LogError(exception, "Unhandled exception: {ExceptionType}", exception.GetType().Name);

        return Problem(
            detail: Environment.IsDevelopment() 
                ? exception.Message 
                : "An error occurred processing your request",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error"
        );
    }
}
```

### Custom Exception Hierarchy

```csharp
// Core domain exceptions
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InvalidEmailException : DomainException
{
    public InvalidEmailException(string message) : base(message) { }
}

public class WeakPasswordException : DomainException
{
    public WeakPasswordException(string message) : base(message) { }
}

public class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string message) : base(message) { }
}

// Application exceptions
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
}

public class InvalidCredentialsException : ApplicationException
{
    public InvalidCredentialsException(string message = "Invalid email or password") : base(message) { }
}

public class TokenExpiredException : ApplicationException
{
    public TokenExpiredException(string message = "Token has expired") : base(message) { }
}
```

---

## 4. Code Quality Standards

### SOLID Principles

- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Derived classes can substitute base classes
- **I**nterface Segregation: Clients depend on specific interfaces
- **D**ependency Inversion: Depend on abstractions, not concretions

### Clean Code

- Method length: < 20 lines
- Class complexity: < 20 methods
- Cyclomatic complexity: < 10
- Comments: Explain *why*, not *what* (code is self-documenting)
- No TODO/FIXME without ticket reference

### XML documentation

- Put **`///` summary (and param/returns where useful) on interfaces** — that is the contract IntelliSense should show.
- On **implementing classes**, use **`/// <inheritdoc />`** on the type and on each member that implements the interface. Do not duplicate interface prose on implementations.
- **Constants**, DTOs/records, controllers, handlers without an interface, and **private members** do not need XML unless there is a compelling reason (keep those self-explanatory or use brief `//` comments for non-obvious logic).
- Constructors are not inherited from interfaces; omit XML on ctor unless the type has no interface and the ctor is public API surface.

### Code Review Checklist

- [ ] No code duplication (DRY principle)
- [ ] Interface members have XML docs; implementations use `<inheritdoc />`
- [ ] All tests passing locally
- [ ] No hardcoded secrets (use configuration)
- [ ] No N+1 query problems
- [ ] Proper error handling (exceptions or result types)
- [ ] Follows naming conventions
- [ ] No unnecessary dependencies

---

## 5. Commit Standards

### Commit Message Format

```
<type>: <subject>

<body>

<footer>
```

- **type:** feat, fix, docs, style, refactor, perf, test, chore
- **subject:** Imperative, capitalized, < 50 chars
- **body:** Wrap at 72 chars, explain *what* and *why*
- **footer:** Reference issues: `Fixes #123`

### Examples

```
feat: add user registration command handler

- Implement RegisterUserRequestHandler with validation
- Add fluent validation rules for strong passwords
- Use bcrypt for password hashing (work factor 12)

Fixes #45
```

```
test: add positive and negative scenarios for email validation

- Data-driven tests for valid/invalid emails
- Edge cases: max length, special characters
- No repetitive test duplication

Relates to #78
```

---

## 6. Project Structure Verification

Run this command to ensure all code is in `src/`:

```bash
# Verify: src/ contains all source, tests/ contains tests
find . -name "*.cs" -not -path "./obj/*" -not -path "./bin/*" | grep -v "^./src/" | grep -v "^./tests/"

# Expected output: (empty)
```

---

## References

- [Microsoft Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [ASP.NET Core Exception Handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [C# Naming Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [BDD with xUnit](https://xunit.net/)
