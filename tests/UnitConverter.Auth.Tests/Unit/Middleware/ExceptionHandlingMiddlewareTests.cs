using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using UnitConverter.Auth.API.Middleware;

namespace UnitConverter.Auth.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for ExceptionHandlingMiddleware.
/// Verifies exception mapping, RFC 7807 compliance, correlation ID propagation,
/// and proper HTTP status code responses.
/// </summary>
[TestClass]
public class ExceptionHandlingMiddlewareTests
{
    private Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private ExceptionHandlingMiddleware _middleware;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var nextDelegate = new RequestDelegate(async context =>
        {
            await Task.CompletedTask;
        });
        _middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);
    }

    [TestMethod]
    [Description("When BadHttpRequestException is thrown, middleware should return 400 Bad Request")]
    public async Task InvokeAsync_WhenBadHttpRequestException_ShouldReturn400()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new BadHttpRequestException("Invalid request body");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [TestMethod]
    [Description("When UnauthorizedAccessException is thrown, middleware should return 401 Unauthorized")]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new UnauthorizedAccessException("Access denied");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [TestMethod]
    [Description("When InvalidOperationException is thrown, middleware should return 403 Forbidden")]
    public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturn403()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new InvalidOperationException("Operation not allowed");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [TestMethod]
    [Description("When RateLimitException is thrown, middleware should return 429 Too Many Requests with Retry-After header")]
    public async Task InvokeAsync_WhenRateLimitException_ShouldReturn429WithRetryAfter()
    {
        // Arrange
        var context = CreateHttpContext();
        var retryAfterSeconds = 30;
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new RateLimitException("Rate limit exceeded", retryAfterSeconds);
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.Should().NotBeEmpty();
        context.Response.Headers.RetryAfter.FirstOrDefault().Should().Be(retryAfterSeconds.ToString());
    }

    [TestMethod]
    [Description("When unhandled exception is thrown, middleware should return 500 Internal Server Error")]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new Exception("Unexpected error");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [TestMethod]
    [Description("Response should conform to RFC 7807 ProblemDetails format")]
    public async Task InvokeAsync_WhenExceptionThrown_ResponseShouldFollowRfc7807()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new BadHttpRequestException("Invalid input");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = ReadResponseBody(context);
        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);

        json.TryGetProperty("type", out var typeProperty).Should().BeTrue();
        json.TryGetProperty("title", out var titleProperty).Should().BeTrue();
        json.TryGetProperty("status", out var statusProperty).Should().BeTrue();
        json.TryGetProperty("detail", out var detailProperty).Should().BeTrue();
        json.TryGetProperty("traceId", out var traceIdProperty).Should().BeTrue();
        json.TryGetProperty("correlationId", out var correlationIdProperty).Should().BeTrue();
        json.TryGetProperty("timestamp", out var timestampProperty).Should().BeTrue();

        statusProperty.GetInt32().Should().Be(400);
    }

    [TestMethod]
    [Description("Correlation ID should be propagated from request headers to response")]
    public async Task InvokeAsync_WhenCorrelationIdInHeaders_ShouldPropagateToResponse()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = correlationId;

        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new BadHttpRequestException("Invalid request");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = ReadResponseBody(context);
        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);

        json.TryGetProperty("correlationId", out var correlationIdProperty).Should().BeTrue();
        correlationIdProperty.GetString().Should().Be(correlationId);
    }

    [TestMethod]
    [Description("Middleware should log exceptions with appropriate severity")]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldLogException()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            throw new BadHttpRequestException("Invalid request");
        });
        var middleware = new ExceptionHandlingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static string ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return reader.ReadToEnd();
    }
}
