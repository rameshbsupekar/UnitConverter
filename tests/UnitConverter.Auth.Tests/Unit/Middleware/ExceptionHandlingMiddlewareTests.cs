using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Middleware;

namespace UnitConverter.UserManagement.Api.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for <see cref="ApiExceptionHandlingMiddleware"/>.
/// </summary>
[TestClass]
public class ExceptionHandlingMiddlewareTests
{
    private Mock<ILogger<ApiExceptionHandlingMiddleware>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiExceptionHandlingMiddleware>>();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn400WithPropertyMessages()
    {
        var context = CreateHttpContext();
        var failures = new[]
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Password", "Password is required")
        };
        var nextDelegate = new RequestDelegate(_ => throw new ValidationException(failures));
        var middleware = CreateMiddleware(nextDelegate);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = ReadResponseBody(context);
        body.Should().Contain("Email: Email is required");
        body.Should().Contain("Password: Password is required");
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
        var middleware = CreateMiddleware(nextDelegate);

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
        var middleware = CreateMiddleware(nextDelegate);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [TestMethod]
    [Description("When InvalidOperationException is thrown in Production, middleware should return 500 with a generic detail")]
    public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturn500WithGenericDetail()
    {
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(_ => throw new InvalidOperationException("Operation not allowed"));
        var middleware = CreateMiddleware(nextDelegate);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = ReadResponseBody(context);
        body.Should().Contain(ProblemTitles.UnexpectedErrorDetail);
        body.Should().NotContain("Operation not allowed");
    }

    [TestMethod]
    [Description("When unhandled exception is thrown in Development, middleware should rethrow for the developer exception page")]
    public async Task InvokeAsync_WhenUnhandledExceptionInDevelopment_ShouldRethrow()
    {
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(_ => throw new Exception("Unexpected error"));
        var middleware = CreateMiddleware(nextDelegate, "Development");

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<Exception>().WithMessage("Unexpected error");
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
        var middleware = CreateMiddleware(nextDelegate);

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
        var middleware = CreateMiddleware(nextDelegate);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var responseBody = ReadResponseBody(context);
        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);
        json.TryGetProperty("detail", out var detailProperty).Should().BeTrue();
        detailProperty.GetString().Should().Be(ProblemTitles.UnexpectedErrorDetail);
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
        var middleware = CreateMiddleware(nextDelegate);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = ReadResponseBody(context);
        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);

        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("title", out _).Should().BeTrue();
        json.TryGetProperty("status", out var statusProperty).Should().BeTrue();
        json.TryGetProperty("detail", out _).Should().BeTrue();
        json.TryGetProperty("traceId", out _).Should().BeTrue();
        json.TryGetProperty("correlationId", out _).Should().BeTrue();

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
        var middleware = CreateMiddleware(nextDelegate);

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
        var middleware = CreateMiddleware(nextDelegate);

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

    private ApiExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        string environmentName = "Production")
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);
        return new ApiExceptionHandlingMiddleware(next, _loggerMock.Object, environment.Object);
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
