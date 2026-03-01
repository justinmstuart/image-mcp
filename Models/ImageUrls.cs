using System.Text.Json.Serialization;

namespace image_mcp.Models;

/// <summary>
/// Represents the URLs for different sizes of an image.
/// </summary>
public class ImageUrls
{
    /// <summary>
    /// Gets or sets the URL for the raw (original) image.
    /// </summary>
    [JsonPropertyName("raw")]
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the full-size image.
    /// </summary>
    [JsonPropertyName("full")]
    public string Full { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the regular-size image.
    /// </summary>
    [JsonPropertyName("regular")]
    public string Regular { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the small-size image.
    /// </summary>
    [JsonPropertyName("small")]
    public string Small { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the thumbnail image.
    /// </summary>
    [JsonPropertyName("thumb")]
    public string Thumb { get; set; } = string.Empty;
}
