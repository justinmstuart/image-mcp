namespace image_mcp.Models;

/// <summary>
/// Represents the result of an image search, including image URLs and metadata.
/// </summary>
public class ImageResult
{
    /// <summary>
    /// Gets or sets the URLs for different image sizes.
    /// </summary>
    public ImageUrls Urls { get; set; } = new();

    /// <summary>
    /// Gets or sets the description of the image.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the alternative description for the image.
    /// </summary>
    public string? AltDescription { get; set; }

    /// <summary>
    /// Gets or sets the error message, if any occurred during the search.
    /// </summary>
    public string? Error { get; set; }
}
