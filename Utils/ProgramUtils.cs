using System.Net.Http.Headers;

using image_mcp.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace image_mcp.Utils;

/// <summary>
/// Shared program setup utilities for configuring logging and common services.
/// </summary>
/// <remarks>
/// These helpers are used to align CLI and MCP server startup so both paths
/// register the same core dependencies.
/// </remarks>
public static class ProgramUtils
{
    /// <summary>
    /// Configures logging for the application host.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <remarks>
    /// Console logging is routed to stderr to avoid interfering with MCP stdio.
    /// </remarks>
    public static void ConfigureLogging(HostApplicationBuilder builder)
    {
        // Disable console logging to prevent interference with MCP stdio protocol
        // But keep logging to stderr for debugging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
    }

    /// <summary>
    /// Registers shared services required by both CLI and MCP server modes.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <remarks>
    /// This binds <see cref="ImageApiOptions"/> from configuration and validates
    /// values on startup, then configures an <see cref="HttpClient"/> with the
    /// image API base address and User-Agent header.
    /// </remarks>
    public static void ConfigureSharedServices(HostApplicationBuilder builder)
    {
        builder.Services.AddOptions<ImageApiOptions>()
            .Bind(builder.Configuration.GetSection("ImageApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<HttpClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ImageApiOptions>>().Value;
            var client = new HttpClient { BaseAddress = new Uri(options.BaseUrl) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("image-search", "1.0"));
            return client;
        });
    }
}