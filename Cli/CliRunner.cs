using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Options;

using Tools;

namespace image_mcp.Cli;

/// <summary>
/// Provides the main entry point for running CLI commands for the image-mcp application.
/// </summary>
public static class CliRunner
{
    /// <summary>
    /// Executes the CLI command based on the provided arguments.
    /// Handles help, version, and search commands, and outputs results in JSON format.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <param name="output">The output writer (defaults to Console.Out).</param>
    /// <param name="error">The error writer (defaults to Console.Error).</param>
    /// <returns>
    /// An integer exit code:
    /// 0 for success,
    /// 1 for partial failure (e.g., search errors),
    /// 2 for usage or argument errors.
    /// </returns>
    public static async Task<int> RunAsync(
        string[] args,
        IServiceProvider services,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        if (!CliUtils.IsCliInvocation(args))
        {
            await error.WriteLineAsync("No command provided.");
            await error.WriteLineAsync(CliUtils.GetUsage());
            return 2;
        }

        var command = args[0];

        if (CliUtils.IsHelpCommand(args))
        {
            await output.WriteLineAsync(CliUtils.GetHelpShell());
            return 0;
        }

        if (CliUtils.IsVersionCommand(args))
        {
            await output.WriteLineAsync(CliUtils.GetVersionText());
            return 0;
        }

        if (!CliUtils.IsSearchCommand(args))
        {
            await error.WriteLineAsync("Unknown command.");
            await error.WriteLineAsync(CliUtils.GetUsage());
            return 2;
        }

        if (args.Length < 2)
        {
            await error.WriteLineAsync("Missing query.");
            await error.WriteLineAsync(CliUtils.GetUsage());
            return 2;
        }

        var query = string.Join(' ', args.Skip(1)).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            await error.WriteLineAsync("Query cannot be empty.");
            await error.WriteLineAsync(CliUtils.GetUsage());
            return 2;
        }

        var client = services.GetRequiredService<HttpClient>();
        var options = services.GetRequiredService<IOptions<ImageApiOptions>>();
        var results = (await ImageSearchTools.SearchImages(client, options, query)).ToList();

        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await output.WriteLineAsync(json);

        var firstError = results.FirstOrDefault(static r => !string.IsNullOrWhiteSpace(r.Error))?.Error;
        if (!string.IsNullOrWhiteSpace(firstError))
        {
            await error.WriteLineAsync(firstError);
            return 1;
        }

        return 0;
    }
}