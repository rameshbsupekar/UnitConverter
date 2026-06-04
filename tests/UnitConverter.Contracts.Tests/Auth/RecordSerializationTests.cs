using System.Text.Json;
using UnitConverter.UserManagement.Contracts.Requests;
using UnitConverter.UserManagement.Contracts.Responses;
using UnitConverter.Common.Contracts.Responses;
using Xunit;

namespace UnitConverter.Contracts.Tests.Auth;

public class RecordSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Fact]
    public void RegisterUserRequest_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var command = new RegisterUserRequest(
            Email: "user@example.com",
            Password: "SecurePassword123!",
            FirstName: "John",
            LastName: "Doe",
            OrganizationName: "ACME Corp",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        string json = JsonSerializer.Serialize(command, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RegisterUserRequest>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(command.Email, deserialized.Email);
        Assert.Equal(command.Password, deserialized.Password);
        Assert.Equal(command.FirstName, deserialized.FirstName);
        Assert.Equal(command.LastName, deserialized.LastName);
        Assert.Equal(command.OrganizationName, deserialized.OrganizationName);
        Assert.Equal(command.IdempotencyKey, deserialized.IdempotencyKey);
    }

    [Fact]
    public void LoginRequest_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var command = new LoginRequest(
            Email: "user@example.com",
            Password: "SecurePassword123!"
        );

        // Act
        string json = JsonSerializer.Serialize(command, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<LoginRequest>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(command.Email, deserialized.Email);
        Assert.Equal(command.Password, deserialized.Password);
    }

    [Fact]
    public void UserResponse_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var response = new UserResponse(
            Id: 1001,
            Email: "user@example.com",
            FirstName: "John",
            LastName: "Doe",
            OrganizationName: "ACME Corp",
            CreatedAt: DateTime.UtcNow,
            Roles: ["Admin", "User"]
        );

        // Act
        string json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<UserResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(response.Id, deserialized.Id);
        Assert.Equal(response.Email, deserialized.Email);
        Assert.Equal(response.FirstName, deserialized.FirstName);
        Assert.Equal(response.LastName, deserialized.LastName);
        Assert.Equal(response.OrganizationName, deserialized.OrganizationName);
        Assert.Equal(response.CreatedAt, deserialized.CreatedAt);
        Assert.Equal(response.Roles, deserialized.Roles);
    }

    [Fact]
    public void TokenResponse_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var issuedAt = DateTime.UtcNow;
        var response = new TokenResponse(
            AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken: "refresh_token_value",
            ExpiresIn: 3600,
            IssuedAt: issuedAt
        );

        // Act
        string json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(response.AccessToken, deserialized.AccessToken);
        Assert.Equal(response.RefreshToken, deserialized.RefreshToken);
        Assert.Equal(response.ExpiresIn, deserialized.ExpiresIn);
        Assert.Equal(response.IssuedAt, deserialized.IssuedAt);
    }

    [Fact]
    public void ErrorResponse_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var response = new ErrorResponse(
            Type: "https://example.com/errors/validation-error",
            Title: "Validation Error",
            Status: 400,
            Detail: "Email is required",
            TraceId: "0HN1GDGJ4FLEH:00000001",
            CorrelationId: "12345678-1234-1234-1234-123456789012"
        );

        // Act
        string json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ErrorResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(response.Type, deserialized.Type);
        Assert.Equal(response.Title, deserialized.Title);
        Assert.Equal(response.Status, deserialized.Status);
        Assert.Equal(response.Detail, deserialized.Detail);
        Assert.Equal(response.TraceId, deserialized.TraceId);
        Assert.Equal(response.CorrelationId, deserialized.CorrelationId);
    }

    [Fact]
    public void RefreshTokenRequest_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var command = new RefreshTokenRequest(
            RefreshToken: "refresh_token_value"
        );

        // Act
        string json = JsonSerializer.Serialize(command, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefreshTokenRequest>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(command.RefreshToken, deserialized.RefreshToken);
    }
}
