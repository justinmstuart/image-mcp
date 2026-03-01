using image_mcp.Cli;
using image_mcp.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Determine startup mode: CLI when arguments are provided, otherwise MCP server mode.

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory
});

if (CliUtils.IsCliMode(args))
{
    var exitCode = await CliRunner.RunAsync(args,builder);
    return exitCode;
}

// Shared setup used by both CLI and MCP server execution paths.
ProgramUtils.ConfigureLogging(builder);
ProgramUtils.ConfigureSharedServices(builder);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var mcpApp = builder.Build();

await mcpApp.RunAsync();
return 0;
