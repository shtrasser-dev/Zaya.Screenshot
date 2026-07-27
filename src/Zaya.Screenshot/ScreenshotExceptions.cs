using System.Globalization;
using Zaya.Primitives;
using Zaya.Screenshot.Constants;

namespace Zaya.Screenshot.Models;

/// <summary>
/// Thrown when a <see cref="Primitives.PixelFormat"/> cannot be mapped to a target graphics API.
/// </summary>
public sealed class ScreenshotPixelFormatNotSupportedException : LocalizedException
{
    private readonly string _formatName;

    /// <summary>
    /// Gets the unsupported format name.
    /// </summary>
    public string FormatName => _formatName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotPixelFormatNotSupportedException"/> class.
    /// </summary>
    public ScreenshotPixelFormatNotSupportedException(string formatName)
        : base(LocalizationConstants.Exceptions.FormatNotSupported)
    {
        _formatName = formatName;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.FormatNotSupported, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _formatName);
    }
}

/// <summary>
/// Thrown when mapping <see cref="Primitives.PixelFormat.Bgr24"/> to SkiaSharp, which has no Bgr24 color type.
/// </summary>
public sealed class ScreenshotSkiaBgr24NotSupportedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotSkiaBgr24NotSupportedException"/> class.
    /// </summary>
    public ScreenshotSkiaBgr24NotSupportedException()
        : base(LocalizationConstants.Exceptions.SkiaBgr24NotSupported) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(
               LocalizationConstants.Exceptions.SkiaBgr24NotSupported, culture)
           ?? base.GetLocalizedMessage(culture);
}
