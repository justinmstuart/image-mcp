using System.Text.Json;

namespace image_mcp.Extensions;

/// <summary>
/// Provides extension methods for <see cref="HttpClient"/> to simplify common operations.
/// </summary>
internal static class HttpClientExt
{
    /// <summary>
    /// Sends a GET request to the specified URI and parses the response as a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>A <see cref="Task{JsonDocument}"/> representing the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the HTTP response is unsuccessful.</exception>
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}