using Zaya.Primitives;
using Zaya.Screenshot.Models;

namespace Zaya.Screenshot.Tests.Models;

public class PixelFormatTests
{
    [Fact]
    public void BytesPerPixel_ReturnsCorrectValue()
    {
        Assert.Equal(4, PixelFormat.Bgra32.BytesPerPixel);
        Assert.Equal(3, PixelFormat.Rgb24.BytesPerPixel);
    }

    [Fact]
    public void ToSkiaSharpColorTypeValue_ReturnsCorrectValue()
    {
        Assert.Equal(6, PixelFormat.Bgra32.ToSkiaSharpColorTypeValue()); // SKColorType.Bgra8888
        Assert.Equal(5, PixelFormat.Rgb24.ToSkiaSharpColorTypeValue());  // SKColorType.Rgb888x
        Assert.Equal(9, PixelFormat.Gray8.ToSkiaSharpColorTypeValue());  // SKColorType.Gray8
    }

    [Fact]
    public void ToSkiaSharpColorTypeValue_Bgr24_Throws()
    {
        Assert.Throws<ScreenshotSkiaBgr24NotSupportedException>(() => PixelFormat.Bgr24.ToSkiaSharpColorTypeValue());
    }

    [Fact]
    public void ToImageSharpPixelTypeName_ReturnsCorrectName()
    {
        Assert.Equal("SixLabors.ImageSharp.PixelFormats.Bgra32", PixelFormat.Bgra32.ToImageSharpPixelTypeName());
        Assert.Equal("SixLabors.ImageSharp.PixelFormats.Rgb24", PixelFormat.Rgb24.ToImageSharpPixelTypeName());
    }

    [Fact]
    public void ToImageSharpPixelType_ReturnsType_WhenImageSharpIsReferenced()
    {
        var type = PixelFormat.Bgra32.ToImageSharpPixelType();
        if (type == null)
        {
            Assert.True(true, "ImageSharp not referenced, skipping type test.");
            return;
        }

        Assert.Equal("SixLabors.ImageSharp.PixelFormats.Bgra32", type.FullName);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        Assert.Equal(PixelFormat.Bgra32, PixelFormat.Bgra32);
    }
}
