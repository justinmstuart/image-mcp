using System.ComponentModel.DataAnnotations;

namespace image_mcp.Options;

/// <summary>
/// Represents configuration options for the image search API.
/// </summary>
public class ImageApiOptions
{
    /// <summary>
    /// Gets or sets the base URL for the image API.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID used for authenticating API requests.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;
}
