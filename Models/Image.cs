using System.Text.Json.Serialization;

namespace image_mcp.Models;

/// <summary>
/// Represents an image result from the image search API.
/// </summary>
public class Image
{
    /// <summary>
    /// Gets or sets the unique identifier for the image.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URLs for different image sizes.
    /// </summary>
    [JsonPropertyName("urls")]
    public ImageUrls Urls { get; set; } = new();

    /// <summary>
    /// Gets or sets the description of the image.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the alternative description for the image.
    /// </summary>
    [JsonPropertyName("alt_description")]
    public string? AltDescription { get; set; }

    /// <summary>
    /// Gets or sets the width of the image in pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the image in pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Gets the <see cref="ImageSize"/> representing the width and height of the image.
    /// </summary>
    public ImageSize Size => new() { Width = Width, Height = Height };

    /// <summary>
    /// Gets or sets the creation date and time of the image.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated date and time of the image.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
