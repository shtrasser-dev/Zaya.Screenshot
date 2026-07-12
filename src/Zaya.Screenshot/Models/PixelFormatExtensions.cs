using Zaya.Primitives;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Extension methods for <see cref="PixelFormat"/> to convert to SkiaSharp and ImageSharp types.
/// </summary>
public static class PixelFormatExtensions
{
    /// <summary>
    /// Converts to SkiaSharp color type value (int).
    /// </summary>
    public static int ToSkiaSharpColorTypeValue(this PixelFormat format)
    {
        return format switch
        {
            var f when f.Equals(PixelFormat.Bgra32) => 5,  // SKColorType.Bgra8888
            var f when f.Equals(PixelFormat.Rgb24) => 11,  // SKColorType.Rgb888x
            var f when f.Equals(PixelFormat.Bgr24) => 11,  // SKColorType.Rgb888x
            var f when f.Equals(PixelFormat.Gray8) => 4,   // SKColorType.Alpha8 (оттенки серого)
            _ => throw new NotSupportedException($"Format '{format.Name}' is not supported.")
        };
    }

    /// <summary>
    /// Gets the ImageSharp pixel type name.
    /// </summary>
    public static string ToImageSharpPixelTypeName(this PixelFormat format)
    {
        return format switch
        {
            var f when f.Equals(PixelFormat.Bgra32) => "SixLabors.ImageSharp.PixelFormats.Bgra32",
            var f when f.Equals(PixelFormat.Rgb24) => "SixLabors.ImageSharp.PixelFormats.Rgb24",
            var f when f.Equals(PixelFormat.Bgr24) => "SixLabors.ImageSharp.PixelFormats.Bgr24",
            var f when f.Equals(PixelFormat.Gray8) => "SixLabors.ImageSharp.PixelFormats.L8",  // L8 = Grayscale
            _ => throw new NotSupportedException($"Format '{format.Name}' is not supported.")
        };
    }

    /// <summary>
    /// Gets the ImageSharp pixel Type object.
    /// </summary>
    public static Type? ToImageSharpPixelType(this PixelFormat format)
    {
        var typeName = format.ToImageSharpPixelTypeName();
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SixLabors.ImageSharp");

        return assembly?.GetType(typeName, false);
    }
}
