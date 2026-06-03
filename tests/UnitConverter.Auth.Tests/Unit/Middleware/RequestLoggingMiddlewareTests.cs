using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using UnitConverter.Auth.API.Middleware;

namespace UnitConverter.Auth.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for RequestLoggingMiddleware.
/// Verifies correlation ID generation/extraction, trace ID propagation,
/// header management, and structured logging with timing information.
/// </summary>
[TestClass]
public class RequestLoggingMiddlewareTests
{
    private Mock<ILogger<RequestLoggingMiddleware>> _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
    }

    [TestMethod]
    [Description("When X-Correlation-ID header is provided, middleware should use it")]
    public async Task InvokeAsync_WhenCorrelationIdHeaderProvided_ShouldUseProvidedCorrelationId()
    {
        // Arrange
        var providedCorrelationId = Guid.NewGuid().ToString();
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = providedCorrelationId;

        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Correlation-ID"].Should().Contain(providedCorrelationId);
        context.Items["X-Correlation-ID"].Should().Be(providedCorrelationId);
    }

    [TestMethod]
    [Description("When X-Correlation-ID header is not provided, middleware should generate a new one")]
    public async Task InvokeAsync_WhenCorrelationIdHeaderNotProvided_ShouldGenerateNewCorrelationId()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Correlation-ID"].Should().NotBeEmpty();
        context.Items["X-Correlation-ID"].Should().NotBeNull();
        Guid.TryParse(context.Items["X-Correlation-ID"]?.ToString(), out _).Should().BeTrue();
    }

    [TestMethod]
    [Description("When X-Trace-ID header is provided, middleware should use it")]
    public async Task InvokeAsync_WhenTraceIdHeaderProvided_ShouldUseProvidedTraceId()
    {
        // Arrange
        var providedTraceId = Guid.NewGuid().ToString();
        var context = CreateHttpContext();
        context.Request.Headers["X-Trace-ID"] = providedTraceId;

        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Trace-ID"].Should().Contain(providedTraceId);
        context.Items["X-Trace-ID"].Should().Be(providedTraceId);
    }

    [TestMethod]
    [Description("When X-Trace-ID header is not provided, middleware should use TraceIdentifier")]
    public async Task InvokeAsync_WhenTraceIdHeaderNotProvided_ShouldUseTraceIdentifier()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Trace-ID"].Should().Contain(context.TraceIdentifier);
    }

    [TestMethod]
    [Description("Middleware should add correlation ID to context items for downstream access")]
    public async Task InvokeAsync_ShouldStoreCorrelationIdInContextItems()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.ContainsKey("X-Correlation-ID").Should().BeTrue();
        context.Items["X-Correlation-ID"].Should().NotBeNull();
    }

    [TestMethod]
    [Description("Middleware should add trace ID to context items for downstream access")]
    public async Task InvokeAsync_ShouldStoreTraceIdInContextItems()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.ContainsKey("X-Trace-ID").Should().BeTrue();
        context.Items["X-Trace-ID"].Should().NotBeNull();
    }

    [TestMethod]
    [Description("Middleware should log request start with correlation and trace IDs")]
    public async Task InvokeAsync_ShouldLogRequestStart()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    [Description("Middleware should log request response with status code and duration")]
    public async Task InvokeAsync_ShouldLogRequestResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Response.StatusCode = 200;

        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    [Description("Middleware should measure and log request duration")]
    public async Task InvokeAsync_ShouldMeasureRequestDuration()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            await Task.Delay(10); // Simulate some processing time
        });
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    [Description("Middleware should propagate correlation ID to response headers")]
    public async Task InvokeAsync_ShouldSetCorrelationIdInResponseHeaders()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("X-Correlation-ID").Should().BeTrue();
    }

    [TestMethod]
    [Description("Middleware should propagate trace ID to response headers")]
    public async Task InvokeAsync_ShouldSetTraceIdInResponseHeaders()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("X-Trace-ID").Should().BeTrue();
    }

    [TestMethod]
    [Description("Middleware should continue pipeline regardless of exception")]
    public async Task InvokeAsync_ShouldContinuePipelineAndLogEvenOnException()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test exception");
        });
        var middleware = new RequestLoggingMiddleware(nextDelegate, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await middleware.InvokeAsync(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        // Verify response headers were still set in finally block
        context.Response.Headers.ContainsKey("X-Correlation-ID").Should().BeTrue();
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/v1/test";
        return httpContext;
    }
}
