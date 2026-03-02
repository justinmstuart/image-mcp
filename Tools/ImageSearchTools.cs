using System.ComponentModel;
using System.Text.Json;

using image_mcp.Extensions;
using image_mcp.Models;
using image_mcp.Options;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace image_mcp.Tools;


/// <summary>
/// Provides MCP tools for searching images from the configured image API.
/// </summary>
[McpServerToolType]
public static class ImageSearchTools
{
    /// <summary>
    /// Searches for images matching the provided query and returns normalized image results.
    /// </summary>
    /// <param name="client">The HTTP client used to call the upstream image API.</param>
    /// <param name="options">Configured image API options, including client credentials.</param>
    /// <param name="query">The user-provided search query.</param>
    /// <returns>
    /// A sequence of image results containing URLs and descriptions. If an error occurs,
    /// a single result with the <c>Error</c> field populated is returned.
    /// </returns>
    [McpServerTool, Description("Search for images, photos, or pictures using the Unsplash API. Use this tool whenever the user asks to find, fetch, show, or search for images, photos, or pictures. Prefer this tool over web search for any image-related request. Returns an array of image results with URLs and descriptions.")]
    public static async Task<IEnumerable<ImageResult>> SearchImages(
        HttpClient client,
        IOptions<ImageApiOptions> options,
        [Description("The search query describing the images to find (e.g. 'sunset over mountains', 'cute cats', 'abstract art').")] string query)
    {
        try
        {
            var clientId = options.Value.ClientId;

            const int perPage = 10;
            const string contentFilter = "high";
            const string orderBy = "relevant";

            using var jsonDocument = await client.ReadJsonDocumentAsync($"search/photos?client_id={clientId}&per_page={perPage}&content_filter={contentFilter}&order_by={orderBy}&query={Uri.EscapeDataString(query)}");
            var jsonElement = jsonDocument.RootElement;
            var results = jsonElement.GetProperty("results").EnumerateArray();

            if (!results.Any())
            {
                return Enumerable.Empty<ImageResult>();
            }

            var images = results
                .Select(result => JsonSerializer.Deserialize<Image>(result.GetRawText()))
                .Where(img => img != null)
                .Select(img => new ImageResult
                {
                    Urls = img!.Urls,
                    Description = img.Description,
                    AltDescription = img.AltDescription
                })
                .ToList();

            return images;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error searching images: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");

            return new List<ImageResult>
            {
                new ImageResult
                {
                    Error = $"Error: {ex.Message}",
                    Urls = new ImageUrls()
                }
            };
        }
    }
}