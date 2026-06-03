using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using UnitConverter.Auth.API.Middleware;

namespace UnitConverter.Auth.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for AuditLoggingMiddleware.
/// Verifies audit logging of authentication endpoints (register, login, logout),
/// user ID extraction, duration tracking, and correlation ID propagation.
/// </summary>
[TestClass]
public class AuditLoggingMiddlewareTests
{
    private Mock<ILogger<AuditLoggingMiddleware>> _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<AuditLoggingMiddleware>>();
    }

    [TestMethod]
    [Description("When request path is /api/v1/auth/register, middleware should audit the request")]
    public async Task InvokeAsync_WhenRegisterEndpoint_ShouldAuditRequest()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/register", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("USER_REGISTRATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("When request path is /api/v1/auth/login, middleware should audit the request")]
    public async Task InvokeAsync_WhenLoginEndpoint_ShouldAuditRequest()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("USER_LOGIN")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("When request path is /api/v1/auth/logout, middleware should audit the request")]
    public async Task InvokeAsync_WhenLogoutEndpoint_ShouldAuditRequest()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/logout", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("USER_LOGOUT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("When request path is non-audited endpoint, middleware should skip audit logging")]
    public async Task InvokeAsync_WhenNonAuditedEndpoint_ShouldNotAudit()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/health", "GET");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audit:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [TestMethod]
    [Description("When user is authenticated, middleware should log user ID in audit trail")]
    public async Task InvokeAsync_WhenUserAuthenticated_ShouldCaptureUserId()
    {
        // Arrange
        var userId = "user123";
        var context = CreateHttpContextWithUser("/api/v1/auth/login", "POST", userId);
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(userId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("When user is not authenticated, middleware should log ANONYMOUS in audit trail")]
    public async Task InvokeAsync_WhenUserNotAuthenticated_ShouldLogAnonymous()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/register", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ANONYMOUS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should capture HTTP method in audit log")]
    public async Task InvokeAsync_ShouldCaptureHttpMethod()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("POST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should capture response status code in audit log")]
    public async Task InvokeAsync_ShouldCaptureStatusCode()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        context.Response.StatusCode = 200;

        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("200")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should measure and log request duration")]
    public async Task InvokeAsync_ShouldMeasureDuration()
    {
        // Arrange
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        var nextDelegate = new RequestDelegate(async _ =>
        {
            await Task.Delay(5); // Simulate some processing
        });
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duration") || v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should propagate correlation ID in audit logs")]
    public async Task InvokeAsync_ShouldPropagateCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        context.Items["X-Correlation-ID"] = correlationId;

        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(correlationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should handle alternative auth paths like /api/auth/login")]
    public async Task InvokeAsync_WhenAlternativeAuthPath_ShouldAudit()
    {
        // Arrange
        var context = CreateHttpContext("/api/auth/login", "POST");
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("USER_LOGIN")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    [Description("Middleware should continue pipeline regardless of audit logging")]
    public async Task InvokeAsync_ShouldContinuePipeline()
    {
        // Arrange
        var pipelelineCalled = false;
        var context = CreateHttpContext("/api/v1/auth/login", "POST");
        var nextDelegate = new RequestDelegate(async _ =>
        {
            pipelelineCalled = true;
            await Task.CompletedTask;
        });
        var middleware = new AuditLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        pipelelineCalled.Should().BeTrue();
    }

    private static HttpContext CreateHttpContext(string path, string method)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Path = path;
        httpContext.Request.Method = method;
        return httpContext;
    }

    private static HttpContext CreateHttpContextWithUser(string path, string method, string userId)
    {
        var httpContext = CreateHttpContext(path, method);
        var claims = new[] { new Claim("sub", userId) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        return httpContext;
    }
}
