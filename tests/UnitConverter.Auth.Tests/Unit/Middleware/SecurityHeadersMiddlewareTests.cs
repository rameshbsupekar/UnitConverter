using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using UnitConverter.Auth.API.Middleware;

namespace UnitConverter.Auth.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware.
/// Verifies HSTS, CSP, and other security headers are properly applied.
/// Tests environment-aware CSP configuration (Production vs Development).
/// </summary>
[TestClass]
public class SecurityHeadersMiddlewareTests
{
    private Mock<ILogger<SecurityHeadersMiddleware>> _loggerMock;
    private Mock<IWebHostEnvironment> _environmentMock;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<SecurityHeadersMiddleware>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
    }

    [TestMethod]
    [Description("Middleware should add HSTS header with correct max-age")]
    public async Task InvokeAsync_ShouldAddHstsHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.StrictTransportSecurity.Should().NotBeEmpty();
        context.Response.Headers.StrictTransportSecurity.FirstOrDefault()
            .Should().Contain("max-age=31536000")
            .And.Contain("includeSubDomains")
            .And.Contain("preload");
    }

    [TestMethod]
    [Description("Middleware should set X-Frame-Options to DENY to prevent clickjacking")]
    public async Task InvokeAsync_ShouldAddXFrameOptionsHeaderAsDeny()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.XFrameOptions.Should().NotBeEmpty();
        context.Response.Headers.XFrameOptions.FirstOrDefault().Should().Be("DENY");
    }

    [TestMethod]
    [Description("Middleware should set X-Content-Type-Options to nosniff")]
    public async Task InvokeAsync_ShouldAddXContentTypeOptionsHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.XContentTypeOptions.Should().NotBeEmpty();
        context.Response.Headers.XContentTypeOptions.FirstOrDefault().Should().Be("nosniff");
    }

    [TestMethod]
    [Description("Middleware should set Referrer-Policy to strict-origin-when-cross-origin")]
    public async Task InvokeAsync_ShouldAddReferrerPolicyHeader()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ReferrerPolicy.Should().NotBeEmpty();
        context.Response.Headers.ReferrerPolicy.FirstOrDefault().Should().Be("strict-origin-when-cross-origin");
    }

    [TestMethod]
    [Description("In Production environment, CSP should be strict without unsafe directives")]
    public async Task InvokeAsync_InProduction_ShouldSetStrictCsp()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(true);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cspHeader = context.Response.Headers.ContentSecurityPolicy.FirstOrDefault();
        cspHeader.Should().NotBeNullOrEmpty();
        cspHeader.Should().NotContain("'unsafe-inline'");
        cspHeader.Should().NotContain("'unsafe-eval'");
        cspHeader.Should().Contain("default-src 'self'");
        cspHeader.Should().Contain("upgrade-insecure-requests");
    }

    [TestMethod]
    [Description("In Development environment, CSP should be permissive for debugging")]
    public async Task InvokeAsync_InDevelopment_ShouldSetPermissiveCsp()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cspHeader = context.Response.Headers.ContentSecurityPolicy.FirstOrDefault();
        cspHeader.Should().NotBeNullOrEmpty();
        cspHeader.Should().Contain("'unsafe-inline'");
        cspHeader.Should().Contain("'unsafe-eval'");
        cspHeader.Should().Contain("http:");
    }

    [TestMethod]
    [Description("All security headers should be present in response")]
    public async Task InvokeAsync_ShouldAddAllSecurityHeaders()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.StrictTransportSecurity.Should().NotBeEmpty();
        context.Response.Headers.XFrameOptions.Should().NotBeEmpty();
        context.Response.Headers.XContentTypeOptions.Should().NotBeEmpty();
        context.Response.Headers.ReferrerPolicy.Should().NotBeEmpty();
        context.Response.Headers.ContentSecurityPolicy.Should().NotBeEmpty();
    }

    [TestMethod]
    [Description("Middleware should call next delegate to continue pipeline")]
    public async Task InvokeAsync_ShouldContinuePipeline()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(false);
        var context = CreateHttpContext();
        var nextCalled = false;

        var nextDelegate = new RequestDelegate(async _ =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [TestMethod]
    [Description("CSP Production header should restrict frame-ancestors to none")]
    public async Task InvokeAsync_ProductionCsp_ShouldRestrictFrameAncestors()
    {
        // Arrange
        _environmentMock.Setup(x => x.IsProduction()).Returns(true);
        var context = CreateHttpContext();
        var nextDelegate = new RequestDelegate(async _ => await Task.CompletedTask);
        var middleware = new SecurityHeadersMiddleware(nextDelegate, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cspHeader = context.Response.Headers.ContentSecurityPolicy.FirstOrDefault();
        cspHeader.Should().Contain("frame-ancestors 'none'");
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }
}
