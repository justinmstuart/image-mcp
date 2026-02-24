using System.IO.Pipelines;
using System.Net;

using Xunit;

using image_mcp.Tests.Helpers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Options;

using Tools;

namespace image_mcp.Tests;

public class McpServerIntegrationTests : IAsyncDisposable
{
    private readonly Pipe _clientToServer = new();
    private readonly Pipe _serverToClient = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _serverTask;
    private McpClient? _client;

    private async Task<McpClient> SetupAsync(HttpClient httpClient)
    {
        var services = new ServiceCollection();
        services.AddMcpServer()
            .WithToolsFromAssembly(typeof(ImageSearchTools).Assembly);
        services.AddSingleton(httpClient);
        services.AddOptions<ImageApiOptions>()
            .Configure(opts =>
            {
                opts.BaseUrl = "https://api.unsplash.com/";
                opts.ClientId = "test_client_id";
            });

        var sp = services.BuildServiceProvider();
        var mcpOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var serverTransport = new StreamServerTransport(
            _clientToServer.Reader.AsStream(),
            _serverToClient.Writer.AsStream(),
            "test-session",
            null);

        var server = McpServer.Create(serverTransport, mcpOptions, null, sp);
        _serverTask = server.RunAsync(_cts.Token);

        var clientTransport = new StreamClientTransport(
            _clientToServer.Writer.AsStream(),
            _serverToClient.Reader.AsStream(),
            null);

        _client = await McpClient.CreateAsync(
            clientTransport,
            new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "test-client", Version = "1.0" }
            });

        return _client;
    }

    [Fact]
    public async Task ListTools_ReturnsSearchImagesTool()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.unsplash.com/") };
        var client = await SetupAsync(httpClient);

        var tools = await client.ListToolsAsync();

        Assert.Contains(tools, t => t.Name == "search_images");
    }

    [Fact]
    public async Task CallSearchImages_ReturnsImageResults_ViaProtocol()
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
                  "description": "Nature photo",
                  "alt_description": "Green forest",
                  "width": 1200,
                  "height": 800,
                  "created_at": "2024-01-01T00:00:00Z",
                  "updated_at": "2024-01-01T00:00:00Z"
                }
              ]
            }
            """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        var client = await SetupAsync(httpClient);

        var result = await client.CallToolAsync(
            "search_images",
            new Dictionary<string, object?> { ["query"] = "nature" });

        Assert.NotNull(result);
        Assert.NotEqual(true, result.IsError);
        Assert.NotEmpty(result.Content);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        if (_serverTask is not null)
        {
            try { await _serverTask; }
            catch (OperationCanceledException) { }
        }

        _cts.Dispose();
    }
}
