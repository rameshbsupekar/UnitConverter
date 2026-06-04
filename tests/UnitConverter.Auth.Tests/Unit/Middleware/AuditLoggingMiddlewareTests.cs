using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using UnitConverter.Common.Attributes;
using UnitConverter.Common.Constants;
using UnitConverter.UserManagement.Api.Middleware;

namespace UnitConverter.UserManagement.Api.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for AuditLoggingMiddleware.
/// Auditing is driven by <see cref="AuditedAttribute"/> endpoint metadata (after routing), not hardcoded paths.
/// </summary>
[TestClass]
public class AuditLoggingMiddlewareTests
{
    private Mock<ILogger<AuditLoggingMiddleware>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<AuditLoggingMiddleware>>();
    }

    [TestMethod]
    [Description("When endpoint is marked [Audited] for register, middleware should audit the request")]
    public async Task InvokeAsync_WhenRegisterEndpoint_ShouldAuditRequest()
    {
        var context = CreateHttpContext(ApiV1Paths.RegisterUser, "POST");
        SetAuditedEndpoint(context, nameof(RegisterAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
    [Description("When endpoint is marked [Audited] for login, middleware should audit the request")]
    public async Task InvokeAsync_WhenLoginEndpoint_ShouldAuditRequest()
    {
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
    [Description("When endpoint is marked [Audited] for logout, middleware should audit the request")]
    public async Task InvokeAsync_WhenLogoutEndpoint_ShouldAuditRequest()
    {
        var context = CreateHttpContext($"{ApiV1Paths.CreateSession}/logout", "POST");
        SetAuditedEndpoint(context, "LogoutAsync");

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
    [Description("When endpoint has no [Audited] metadata, middleware should skip audit logging")]
    public async Task InvokeAsync_WhenNonAuditedEndpoint_ShouldNotAudit()
    {
        var context = CreateHttpContext(ApiV1Paths.Health, "GET");

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
        var userId = "user123";
        var context = CreateHttpContextWithUser(ApiV1Paths.CreateSession, "POST", userId);
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
        var context = CreateHttpContext(ApiV1Paths.RegisterUser, "POST");
        SetAuditedEndpoint(context, nameof(RegisterAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        context.Response.StatusCode = 200;
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(async _ => await Task.Delay(5));

        await middleware.InvokeAsync(context);

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
        var correlationId = Guid.NewGuid().ToString();
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        context.Items["X-Correlation-ID"] = correlationId;
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

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
    [Description("Paths without a resolved [Audited] endpoint are not audited (routing/versioning owns matching)")]
    public async Task InvokeAsync_WhenNoAuditedEndpointMetadata_ShouldNotAudit()
    {
        var context = CreateHttpContext("/api/sessions", "POST");

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [TestMethod]
    [Description("Middleware should continue pipeline regardless of audit logging")]
    public async Task InvokeAsync_ShouldContinuePipeline()
    {
        var pipelineCalled = false;
        var context = CreateHttpContext(ApiV1Paths.CreateSession, "POST");
        SetAuditedEndpoint(context, nameof(LoginAsync));

        var middleware = CreateMiddleware(_ =>
        {
            pipelineCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        pipelineCalled.Should().BeTrue();
    }

    private AuditLoggingMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, _loggerMock.Object);

    private static void SetAuditedEndpoint(HttpContext context, string actionName)
    {
        var metadata = new EndpointMetadataCollection(
            new AuditedAttribute(),
            new ControllerActionDescriptor
            {
                ActionName = actionName,
                ControllerName = "Auth"
            });

        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, $"Auth.{actionName}"));
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
        httpContext.User = new ClaimsPrincipal(identity);
        return httpContext;
    }

    private static Task RegisterAsync() => Task.CompletedTask;

    private static Task LoginAsync() => Task.CompletedTask;
}
