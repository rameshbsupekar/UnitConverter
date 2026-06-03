using UnitConverter.Contracts.Auth.Commands;
using UnitConverter.Contracts.Auth.Responses;
using Xunit;

namespace UnitConverter.Contracts.Tests.Auth;

public class RecordImmutabilityAndPatternsTests
{
    [Fact]
    public void RegisterUserCommand_RecordIsImmutable()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "user@example.com",
            Password: "Password123!",
            FirstName: "John",
            LastName: "Doe",
            OrganizationName: "ACME"
        );

        // Act - Create a "modified" record using 'with' expression
        var modifiedCommand = command with { Email = "newemail@example.com" };

        // Assert - Original remains unchanged, new record created
        Assert.Equal("user@example.com", command.Email);
        Assert.Equal("newemail@example.com", modifiedCommand.Email);
        Assert.NotEqual(command, modifiedCommand);
    }

    [Fact]
    public void UserResponse_RecordSupportsPatternMatching()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var response = new UserResponse(
            Id: userId,
            Email: "admin@example.com",
            FirstName: "Jane",
            LastName: "Smith",
            Roles: ["Admin", "User"]
        );

        // Act - Pattern matching on record
        var result = response switch
        {
            UserResponse { Roles.Length: > 0 } => "Has roles",
            _ => "No roles"
        };

        // Assert
        Assert.Equal("Has roles", result);
    }

    [Fact]
    public void LoginCommand_RecordEquality()
    {
        // Arrange
        var command1 = new LoginCommand(Email: "user@example.com", Password: "Pass123!");
        var command2 = new LoginCommand(Email: "user@example.com", Password: "Pass123!");
        var command3 = new LoginCommand(Email: "other@example.com", Password: "Pass123!");

        // Act & Assert
        Assert.Equal(command1, command2);
        Assert.NotEqual(command1, command3);
    }

    [Fact]
    public void TokenResponse_RecordHashCode()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var response1 = new TokenResponse(
            AccessToken: "token1",
            RefreshToken: "refresh1",
            ExpiresIn: 3600,
            IssuedAt: now
        );
        var response2 = new TokenResponse(
            AccessToken: "token1",
            RefreshToken: "refresh1",
            ExpiresIn: 3600,
            IssuedAt: now
        );

        // Act
        var set = new HashSet<TokenResponse> { response1 };

        // Assert - Equal records have same hash code
        Assert.Equal(response1.GetHashCode(), response2.GetHashCode());
        Assert.True(set.Contains(response2));
    }

    [Fact]
    public void ErrorResponse_RecordDeconstructionAndReconstruction()
    {
        // Arrange
        var error = new ErrorResponse(
            Type: "ValidationError",
            Title: "Validation Failed",
            Status: 400,
            Detail: "Email is required",
            TraceId: "trace-123",
            CorrelationId: "corr-456"
        );

        // Act - Deconstruct and verify all properties accessible
        var (type, title, status, detail, traceId, correlationId) = error;

        // Assert
        Assert.Equal("ValidationError", type);
        Assert.Equal("Validation Failed", title);
        Assert.Equal(400, status);
        Assert.Equal("Email is required", detail);
        Assert.Equal("trace-123", traceId);
        Assert.Equal("corr-456", correlationId);
    }

    [Fact]
    public void RevokeTokenCommand_RecordToString()
    {
        // Arrange
        var command = new RevokeTokenCommand(RefreshToken: "refresh_token_xyz");

        // Act
        string representation = command.ToString();

        // Assert - Records have meaningful string representation
        Assert.Contains("RevokeTokenCommand", representation);
        Assert.Contains("refresh_token_xyz", representation);
    }
}
