using System.Net;
using System.Text;

using image_mcp.Cli;
using image_mcp.Tests.Helpers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Options;

using Xunit;

namespace image_mcp.Tests;

public class CliRunnerTests
{
  private static ServiceProvider CreateServiceProvider(HttpClient client, string clientId = "test_client_id")
  {
    var services = new ServiceCollection();
    services.AddSingleton(client);
    services.AddOptions<ImageApiOptions>()
        .Configure(options =>
        {
          options.BaseUrl = "https://api.unsplash.com/";
          options.ClientId = clientId;
        });

    return services.BuildServiceProvider();
  }

  [Fact]
  public void IsCliInvocation_ReturnsTrue_ForSearchCommand()
  {
    Assert.True(CliRunner.IsCliInvocation(["search", "cats"]));
    Assert.True(CliRunner.IsCliInvocation(["SEARCH", "cats"]));
    Assert.True(CliRunner.IsCliInvocation(["--help"]));
    Assert.True(CliRunner.IsCliInvocation(["--version"]));
  }

  [Fact]
  public void IsCliInvocation_ReturnsFalse_WhenNoArgsProvided()
  {
    Assert.False(CliRunner.IsCliInvocation([]));
  }

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

    var handler = new FakeHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };

    using var serviceProvider = CreateServiceProvider(client);
    using var outputWriter = new StringWriter();
    using var errorWriter = new StringWriter();

    var exitCode = await CliRunner.RunAsync(["search", "nature"], serviceProvider, outputWriter, errorWriter);

    Assert.Equal(0, exitCode);
    Assert.Contains("\"description\": \"A test image\"", outputWriter.ToString());
    Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
  }

  [Fact]
  public async Task RunAsync_ReturnsTwo_WhenQueryMissing()
  {
    var client = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
    {
      BaseAddress = new Uri("https://api.unsplash.com/")
    };

    using var serviceProvider = CreateServiceProvider(client);
    using var outputWriter = new StringWriter();
    using var errorWriter = new StringWriter();

    var exitCode = await CliRunner.RunAsync(["search"], serviceProvider, outputWriter, errorWriter);

    Assert.Equal(2, exitCode);
    Assert.Contains("Usage: image-mcp <command> [options]", errorWriter.ToString());
  }

  [Fact]
  public async Task RunAsync_ReturnsZero_AndPrintsHelpShell_ForHelpCommand()
  {
    var client = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
    {
      BaseAddress = new Uri("https://api.unsplash.com/")
    };

    using var serviceProvider = CreateServiceProvider(client);
    using var outputWriter = new StringWriter();
    using var errorWriter = new StringWriter();

    var exitCode = await CliRunner.RunAsync(["--help"], serviceProvider, outputWriter, errorWriter);

    Assert.Equal(0, exitCode);
    Assert.Contains("Commands:", outputWriter.ToString());
    Assert.Contains("More commands coming soon.", outputWriter.ToString());
    Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
  }

  [Fact]
  public async Task RunAsync_ReturnsZero_AndPrintsVersion_ForVersionCommand()
  {
    var client = new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
    {
      BaseAddress = new Uri("https://api.unsplash.com/")
    };

    using var serviceProvider = CreateServiceProvider(client);
    using var outputWriter = new StringWriter();
    using var errorWriter = new StringWriter();

    var exitCode = await CliRunner.RunAsync(["--version"], serviceProvider, outputWriter, errorWriter);

    Assert.Equal(0, exitCode);
    Assert.StartsWith("image-mcp ", outputWriter.ToString().Trim());
    Assert.True(string.IsNullOrWhiteSpace(errorWriter.ToString()));
  }

  [Fact]
  public async Task RunAsync_ReturnsOne_WhenSearchReturnsError()
  {
    var handler = new FakeHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.Unauthorized));
    var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };

    using var serviceProvider = CreateServiceProvider(client);
    using var outputWriter = new StringWriter();
    using var errorWriter = new StringWriter();

    var exitCode = await CliRunner.RunAsync(["search", "nature"], serviceProvider, outputWriter, errorWriter);

    Assert.Equal(1, exitCode);
    Assert.Contains("Error:", outputWriter.ToString());
    Assert.Contains("Error:", errorWriter.ToString());
  }
}