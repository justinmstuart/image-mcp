using System.Net;
using System.Net.Sockets;
using System.Text;

using image_mcp.Cli;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace image_mcp.Tests;

/// <summary>
/// Contains unit tests for the <see cref="CliRunner"/> class, verifying CLI command handling and output.
/// </summary>
public class CliRunnerTests
{
    /// <summary>
    /// Creates a <see cref="HostApplicationBuilder"/> with the required dependencies for testing.
    /// </summary>
    /// <param name="baseAddress">The base address for the HTTP client.</param>
    /// <param name="clientId">The client ID for the API (default: "test_client_id").</param>
    /// <returns>A configured <see cref="HostApplicationBuilder"/>.</returns>
    private static HostApplicationBuilder CreateBuilder(Uri baseAddress, string clientId = "test_client_id")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ImageApi:BaseUrl"] = baseAddress.ToString(),
            ["ImageApi:ClientId"] = clientId
        });

        return builder;
    }

    /// <summary>
    /// Tests that <see cref="CliUtils.IsCliInvocation"/> returns true for valid CLI commands.
    /// </summary>
    [Fact]
    public void IsCliInvocation_ReturnsTrue_ForSearchCommand()
    {
        Assert.True(CliUtils.IsCliInvocation(["search", "cats"]));
        Assert.True(CliUtils.IsCliInvocation(["SEARCH", "cats"]));
        Assert.True(CliUtils.IsCliInvocation(["--help"]));
        Assert.True(CliUtils.IsCliInvocation(["--version"]));
    }

    /// <summary>
    /// Tests that <see cref="CliUtils.IsCliInvocation"/> returns false when no arguments are provided.
    /// </summary>
    [Fact]
    public void IsCliInvocation_ReturnsFalse_WhenNoArgsProvided()
    {
        Assert.False(CliUtils.IsCliInvocation([]));
    }

    /// <summary>
    /// Tests that <see cref="CliRunner.RunAsync"/> returns 0 and prints JSON output on a successful search.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsZero_AndPrintsJson_OnSuccess()
    {
        const string json = """
            {
              "results": [
                {
                  "id": "abc123",
                  "urls": {
                    "raw": "https://example.com/raw",
                    "full": "https://example.com/full",
                    "regular": "https://example.com/regular",
                    "small": "https://example.com/small",
                    "thumb": "https://example.com/thumb"
                  },
                  "description": "A test image",
                  "alt_description": "Test alt",
                  "width": 800,
                  "height": 600,
                  "created_at": "2024-01-01T00:00:00Z",
                  "updated_at": "2024-01-01T00:00:00Z"
                }
              ]
            }
            """;

        await using var server = new OneShotHttpServer(HttpStatusCode.OK, json, "application/json");
        var builder = CreateBuilder(server.BaseAddress);

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = await CliRunner.RunAsync(["search", "nature"], builder, outputWriter, errorWriter);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"description\": \"A test image\"", outputWriter.ToString());
        Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
    }

    /// <summary>
    /// Tests that <see cref="CliRunner.RunAsync"/> returns 2 and prints usage when the query is missing.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsTwo_WhenQueryMissing()
    {
        var builder = CreateBuilder(new Uri("http://127.0.0.1/"));
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = await CliRunner.RunAsync(["search"], builder, outputWriter, errorWriter);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage: image-mcp <command> [options]", errorWriter.ToString());
    }

    /// <summary>
    /// Tests that <see cref="CliRunner.RunAsync"/> returns 0 and prints help text for the help command.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsZero_AndPrintsHelpShell_ForHelpCommand()
    {
        var builder = CreateBuilder(new Uri("http://127.0.0.1/"));
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = await CliRunner.RunAsync(["--help"], builder, outputWriter, errorWriter);

        Assert.Equal(0, exitCode);
        Assert.Contains("Commands:", outputWriter.ToString());
        Assert.Contains("More commands coming soon.", outputWriter.ToString());
        Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
    }

    /// <summary>
    /// Tests that <see cref="CliRunner.RunAsync"/> returns 0 and prints version information for the version command.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsZero_AndPrintsVersion_ForVersionCommand()
    {
        var builder = CreateBuilder(new Uri("http://127.0.0.1/"));
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = await CliRunner.RunAsync(["--version"], builder, outputWriter, errorWriter);

        Assert.Equal(0, exitCode);
        Assert.StartsWith("image-mcp ", outputWriter.ToString().Trim());
        Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
    }

    /// <summary>
    /// Tests that <see cref="CliRunner.RunAsync"/> returns 1 and prints error output when the search returns an error.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsOne_WhenSearchReturnsError()
    {
        await using var server = new OneShotHttpServer(HttpStatusCode.Unauthorized, null, "text/plain");
        var builder = CreateBuilder(server.BaseAddress);

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = await CliRunner.RunAsync(["search", "nature"], builder, outputWriter, errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Contains("Error:", outputWriter.ToString());
        Assert.Contains("Error:", errorWriter.ToString());
    }

    private sealed class OneShotHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _serveTask;
        private readonly byte[] _responseBytes;

        public OneShotHttpServer(HttpStatusCode statusCode, string? body, string contentType)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();

            BaseAddress = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

            var bodyBytes = body is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
            var header = $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
                         $"Content-Type: {contentType}\r\n" +
                         $"Content-Length: {bodyBytes.Length}\r\n" +
                         "Connection: close\r\n\r\n";

            _responseBytes = Encoding.ASCII.GetBytes(header).Concat(bodyBytes).ToArray();
            _serveTask = Task.Run(ServeOnceAsync);
        }

        public Uri BaseAddress { get; }

        private async Task ServeOnceAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();

                // Read until end of headers to keep the client happy.
                var buffer = new byte[1024];
                var headerEnd = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
                var total = 0;

                while (true)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }

                    total += read;
                    if (total >= 4 && ContainsHeaderTerminator(buffer, read, headerEnd))
                    {
                        break;
                    }
                }

                await stream.WriteAsync(_responseBytes, 0, _responseBytes.Length);
            }
            catch (ObjectDisposedException)
            {
                // Listener was stopped during teardown.
            }
        }

        private static bool ContainsHeaderTerminator(byte[] buffer, int read, byte[] terminator)
        {
            for (var i = 0; i <= read - terminator.Length; i++)
            {
                var match = true;
                for (var j = 0; j < terminator.Length; j++)
                {
                    if (buffer[i + j] != terminator[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            try
            {
                await _serveTask;
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}

