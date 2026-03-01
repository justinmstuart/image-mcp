using System;
using System.Reflection;

namespace image_mcp.Cli;

/// <summary>
/// Provides utility methods for parsing and handling CLI commands for the image-mcp application.
/// </summary>
public static class CliUtils
{
    public static readonly Func<string[], bool> IsCliMode = args => args.Length > 0;
    
    /// <summary>
    /// Determines if the CLI was invoked with any arguments.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>True if arguments are present; otherwise, false.</returns>
    public static bool IsCliInvocation(string[] args)
    {
        return args.Length > 0;
    }

    /// <summary>
    /// Determines if the command is a help command (e.g., --help, -h, help).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>True if the command is a help command; otherwise, false.</returns>
    public static bool IsHelpCommand(string[] args)
    {
        if (args.Length == 0) return false;
        var command = args[0];
        return string.Equals(command, "--help", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(command, "-h", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(command, "help", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the command is a version command (e.g., --version, -v, version).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>True if the command is a version command; otherwise, false.</returns>
    public static bool IsVersionCommand(string[] args)
    {
        if (args.Length == 0) return false;
        var command = args[0];
        return string.Equals(command, "--version", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(command, "-v", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(command, "version", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the command is a search command.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>True if the command is "search"; otherwise, false.</returns>
    public static bool IsSearchCommand(string[] args)
    {
        if (args.Length == 0) return false;
        var command = args[0];
        return string.Equals(command, "search", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the usage string for the CLI.
    /// </summary>
    /// <returns>A usage string describing the CLI syntax.</returns>
    public static string GetUsage()
    {
        return "Usage: image-mcp <command> [options]";
    }

    /// <summary>
    /// Gets the help text for the CLI shell.
    /// </summary>
    /// <returns>A help string describing available commands.</returns>
    public static string GetHelpShell()
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

    /// <summary>
    /// Gets the version text for the CLI, including the application version.
    /// </summary>
    /// <returns>A string containing the CLI version.</returns>
    public static string GetVersionText()
    {
        var assembly = typeof(CliRunner).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var normalizedVersion = informationalVersion?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "unknown";
        return $"image-mcp {normalizedVersion}";
    }
}
