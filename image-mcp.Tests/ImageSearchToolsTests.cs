using System.Net;

using image_mcp.Tests.Helpers;

using Xunit;

using Microsoft.Extensions.Options;

using Options;

using Tools;

namespace image_mcp.Tests;

public class ImageSearchToolsTests
{
    private static HttpClient CreateFakeHttpClient(string jsonResponse, Uri? baseAddress = null)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            });
        return new HttpClient(handler) { BaseAddress = baseAddress ?? new Uri("https://api.unsplash.com/") };
    }

    private static IOptions<ImageApiOptions> CreateOptions(string clientId = "test_client_id") =>
        Microsoft.Extensions.Options.Options.Create(new ImageApiOptions
        {
            BaseUrl = "https://api.unsplash.com/",
            ClientId = clientId
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
        Assert.Equal("https://example.com/raw", list[0].Urls.Raw);
        Assert.Equal("https://example.com/full", list[0].Urls.Full);
        Assert.Equal("https://example.com/small", list[0].Urls.Small);
        Assert.Equal("https://example.com/thumb", list[0].Urls.Thumb);
        Assert.Null(list[0].Error);
    }

    [Fact]
    public async Task SearchImages_ReturnsMultipleResults_WhenApiResponseHasMultipleResults()
    {
        const string json = """
            {
              "results": [
                {
                  "id": "img1",
                  "urls": { "raw": "", "full": "", "regular": "https://a.com/1", "small": "", "thumb": "" },
                  "description": "First image",
                  "alt_description": null,
                  "width": 100, "height": 100,
                  "created_at": "2024-01-01T00:00:00Z", "updated_at": "2024-01-01T00:00:00Z"
                },
                {
                  "id": "img2",
                  "urls": { "raw": "", "full": "", "regular": "https://a.com/2", "small": "", "thumb": "" },
                  "description": null,
                  "alt_description": "Second alt",
                  "width": 200, "height": 200,
                  "created_at": "2024-01-01T00:00:00Z", "updated_at": "2024-01-01T00:00:00Z"
                }
              ]
            }
            """;

        var client = CreateFakeHttpClient(json);
        var options = CreateOptions();

        var results = await ImageSearchTools.SearchImages(client, options, "test");

        var list = results.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("First image", list[0].Description);
        Assert.Null(list[0].AltDescription);
        Assert.Null(list[1].Description);
        Assert.Equal("Second alt", list[1].AltDescription);
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

    [Fact]
    public async Task SearchImages_ReturnsErrorResult_WhenServerReturns500()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        var options = CreateOptions();

        var results = await ImageSearchTools.SearchImages(client, options, "cars");

        var list = results.ToList();
        Assert.Single(list);
        Assert.NotNull(list[0].Error);
        Assert.Empty(list[0].Urls.Raw);
    }

    [Fact]
    public async Task SearchImages_UsesClientIdFromOptions()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            const string emptyResults = """{"results": []}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(emptyResults, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        var options = CreateOptions("my_custom_client_id");

        await ImageSearchTools.SearchImages(client, options, "test");

        Assert.NotNull(capturedRequest);
        Assert.Contains("my_custom_client_id", capturedRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task SearchImages_UrlEncodesQuery()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            const string emptyResults = """{"results": []}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(emptyResults, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        var options = CreateOptions();

        await ImageSearchTools.SearchImages(client, options, "cat & dog");

        Assert.NotNull(capturedRequest);
        Assert.Contains("cat", capturedRequest!.RequestUri!.Query);
        Assert.DoesNotContain(" ", capturedRequest.RequestUri.Query);
    }
}
