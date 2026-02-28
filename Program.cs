using System.Net.Http.Headers;

using image_mcp.Cli;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Options;

var isCliMode = args.Length > 0;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory
});

ConfigureLogging(builder);
ConfigureSharedServices(builder);

if (isCliMode)
{
    using var cliApp = builder.Build();
    var exitCode = await CliRunner.RunAsync(args, cliApp.Services);
    return exitCode;
}

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var mcpApp = builder.Build();

await mcpApp.RunAsync();
return 0;

static void ConfigureLogging(HostApplicationBuilder builder)
{
    // Disable console logging to prevent interference with MCP stdio protocol
    // But keep logging to stderr for debugging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
}

static void ConfigureSharedServices(HostApplicationBuilder builder)
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
