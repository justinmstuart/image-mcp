using System.Net;

using image_mcp.Tests.Helpers;

using Xunit;

using Microsoft.Extensions.Options;

using Options;

using Tools;

namespace image_mcp.Tests;

public class ImageSearchToolsTests
{
    private static HttpClient CreateFakeHttpClient(string jsonResponse)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            });
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
    }

    private static IOptions<ImageApiOptions> CreateOptions() =>
        Microsoft.Extensions.Options.Options.Create(new ImageApiOptions
        {
            BaseUrl = "https://api.unsplash.com/",
            ClientId = "test_client_id"
        });

    [Fact]
    public async Task SearchImages_ReturnsResults_WhenApiResponseHasResults()
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

        var client = CreateFakeHttpClient(json);
        var options = CreateOptions();

        var results = await ImageSearchTools.SearchImages(client, options, "nature");

        var list = results.ToList();
        Assert.Single(list);
        Assert.Equal("A test image", list[0].Description);
        Assert.Equal("Test alt", list[0].AltDescription);
        Assert.Equal("https://example.com/regular", list[0].Urls.Regular);
        Assert.Null(list[0].Error);
    }

    [Fact]
    public async Task SearchImages_ReturnsEmpty_WhenApiResponseHasNoResults()
    {
        const string json = """{"results": []}""";

        var client = CreateFakeHttpClient(json);
        var options = CreateOptions();

        var results = await ImageSearchTools.SearchImages(client, options, "xyzzy");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchImages_ReturnsErrorResult_WhenApiFails()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        var options = CreateOptions();

        var results = await ImageSearchTools.SearchImages(client, options, "nature");

        var list = results.ToList();
        Assert.Single(list);
        Assert.NotNull(list[0].Error);
        Assert.StartsWith("Error:", list[0].Error);
    }
}
