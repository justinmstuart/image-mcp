using System.Text.Json;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Options;

using Tools;

namespace image_mcp.Cli;

public static class CliRunner
{
    public static bool IsCliInvocation(string[] args)
    {
        return args.Length > 0;
    }

    public static async Task<int> RunAsync(
        string[] args,
        IServiceProvider services,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        if (!IsCliInvocation(args))
        {
            await error.WriteLineAsync("No command provided.");
            await error.WriteLineAsync(GetUsage());
            return 2;
        }

        var command = args[0];

        if (string.Equals(command, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "-h", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync(GetHelpShell());
            return 0;
        }

        if (string.Equals(command, "--version", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "-v", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "version", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync(GetVersionText());
            return 0;
        }

        if (!string.Equals(command, "search", StringComparison.OrdinalIgnoreCase))
        {
            await error.WriteLineAsync("Unknown command.");
            await error.WriteLineAsync(GetUsage());
            return 2;
        }

        if (args.Length < 2)
        {
            await error.WriteLineAsync("Missing query.");
            await error.WriteLineAsync(GetUsage());
            return 2;
        }

        var query = string.Join(' ', args.Skip(1)).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            await error.WriteLineAsync("Query cannot be empty.");
            await error.WriteLineAsync(GetUsage());
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

    private static string GetUsage()
    {
        return "Usage: image-mcp <command> [options]";
    }

    private static string GetHelpShell()
    {
        return """
Usage: image-mcp <command> [options]

Commands:
  search <query>   Search images and output JSON.
  --version        Show CLI version.
  --help           Show this help text.

More commands coming soon.
""";
    }

    private static string GetVersionText()
    {
        var assembly = typeof(CliRunner).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var normalizedVersion = informationalVersion?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "unknown";
        return $"image-mcp {normalizedVersion}";
    }
}