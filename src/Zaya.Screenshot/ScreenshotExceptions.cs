namespace Zaya.Screenshot.Models;

/// <summary>
/// Thrown when a <see cref="Primitives.PixelFormat"/> cannot be mapped to a target graphics API.
/// </summary>
public sealed class ScreenshotPixelFormatNotSupportedException : NotSupportedException
{
    /// <summary>
    /// Gets the unsupported format name.
    /// </summary>
    public string FormatName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotPixelFormatNotSupportedException"/> class.
    /// </summary>
    /// <param name="formatName">Name of the unsupported pixel format.</param>
    public ScreenshotPixelFormatNotSupportedException(string formatName)
        : base($"Format '{formatName}' is not supported.")
    {
        FormatName = formatName;
    }
}

/// <summary>
/// Thrown when mapping <see cref="Primitives.PixelFormat.Bgr24"/> to SkiaSharp, which has no Bgr24 color type.
/// </summary>
public sealed class ScreenshotSkiaBgr24NotSupportedException : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotSkiaBgr24NotSupportedException"/> class.
    /// </summary>
    public ScreenshotSkiaBgr24NotSupportedException()
        : base("SkiaSharp has no Bgr24 color type; convert to Bgra32 or Rgb24 first.")
    {
    }
}
