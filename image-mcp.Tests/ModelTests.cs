using System.Text.Json;

using Xunit;

using image_mcp.Models;

namespace image_mcp.Tests;

public class ModelTests
{
    [Fact]
    public void Image_Size_ReturnsCorrectWidthAndHeight()
    {
        var image = new Image { Width = 1920, Height = 1080 };

        var size = image.Size;

        Assert.Equal(1920, size.Width);
        Assert.Equal(1080, size.Height);
    }

    [Fact]
    public void Image_DeserializesFromJson()
    {
        const string json = """
            {
              "id": "xyz",
              "urls": {
                "raw": "https://r.com",
                "full": "https://f.com",
                "regular": "https://re.com",
                "small": "https://s.com",
                "thumb": "https://t.com"
              },
              "description": "Desc",
              "alt_description": "Alt",
              "width": 640,
              "height": 480,
              "created_at": "2025-06-01T12:00:00Z",
              "updated_at": "2025-06-02T12:00:00Z"
            }
            """;

        var image = JsonSerializer.Deserialize<Image>(json);

        Assert.NotNull(image);
        Assert.Equal("xyz", image!.Id);
        Assert.Equal("Desc", image.Description);
        Assert.Equal("Alt", image.AltDescription);
        Assert.Equal(640, image.Width);
        Assert.Equal(480, image.Height);
        Assert.Equal(640, image.Size.Width);
        Assert.Equal(480, image.Size.Height);
        Assert.Equal("https://re.com", image.Urls.Regular);
    }

    [Fact]
    public void ImageSize_DefaultValues_AreZero()
    {
        var size = new ImageSize();

        Assert.Equal(0, size.Width);
        Assert.Equal(0, size.Height);
    }

    [Fact]
    public void ImageSize_CanSetAndGetValues()
    {
        var size = new ImageSize { Width = 1280, Height = 720 };

        Assert.Equal(1280, size.Width);
        Assert.Equal(720, size.Height);
    }
}
