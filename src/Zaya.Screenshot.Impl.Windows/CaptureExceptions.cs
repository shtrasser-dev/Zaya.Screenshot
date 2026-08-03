using System.Globalization;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Constants;

namespace Zaya.Screenshot.Impl.Windows;

/// <summary>
/// Thrown when Windows Graphics Capture is not supported on the current system.
/// </summary>
public sealed class CaptureNotSupportedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureNotSupportedException"/> class.
    /// </summary>
    public CaptureNotSupportedException() : base(LocalizationConstants.Exceptions.NotSupported) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.NotSupported, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when a <c>GraphicsCaptureItem</c> cannot be created.
/// </summary>
public sealed class CaptureItemCreateException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureItemCreateException"/> class.
    /// </summary>
    public CaptureItemCreateException() : base(LocalizationConstants.Exceptions.ItemCreateFailed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.ItemCreateFailed, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when a capture item cannot be created for a specific window handle.
/// </summary>
public sealed class CaptureWindowItemCreateException : LocalizedException
{
    private readonly string _hwndHex;

    /// <summary>
    /// Gets the window handle as a hex string.
    /// </summary>
    public string WindowHandleHex => _hwndHex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureWindowItemCreateException"/> class.
    /// </summary>
    /// <param name="hwnd">Window handle that failed.</param>
    public CaptureWindowItemCreateException(nint hwnd)
        : base(LocalizationConstants.Exceptions.WindowItemCreateFailed)
    {
        _hwndHex = $"0x{hwnd:X}";
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.WindowItemCreateFailed, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _hwndHex);
    }
}

/// <summary>
/// Thrown when the capture region type is not supported.
/// </summary>
public sealed class CaptureRegionNotSupportedException : LocalizedException
{
    private readonly string _regionType;

    /// <summary>
    /// Gets the unsupported region type name.
    /// </summary>
    public string RegionType => _regionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureRegionNotSupportedException"/> class.
    /// </summary>
    /// <param name="regionType">CLR type name of the region.</param>
    public CaptureRegionNotSupportedException(string regionType)
        : base(LocalizationConstants.Exceptions.RegionNotSupported)
    {
        _regionType = regionType;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.RegionNotSupported, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _regionType);
    }
}

/// <summary>
/// Thrown when the requested display index is outside the available monitor range.
/// </summary>
public sealed class CaptureDisplayIndexException : LocalizedException
{
    private readonly int _displayIndex;
    private readonly int _monitorCount;

    /// <summary>
    /// Gets the requested display index.
    /// </summary>
    public int DisplayIndex => _displayIndex;

    /// <summary>
    /// Gets the number of available monitors.
    /// </summary>
    public int MonitorCount => _monitorCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureDisplayIndexException"/> class.
    /// </summary>
    public CaptureDisplayIndexException(int displayIndex, int monitorCount)
        : base(LocalizationConstants.Exceptions.DisplayIndexOutOfRange)
    {
        _displayIndex = displayIndex;
        _monitorCount = monitorCount;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.DisplayIndexOutOfRange, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _displayIndex, _monitorCount);
    }
}

/// <summary>
/// Thrown when no capture frame arrives within the session timeout.
/// </summary>
public sealed class CaptureFrameTimeoutException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureFrameTimeoutException"/> class.
    /// </summary>
    public CaptureFrameTimeoutException() : base(LocalizationConstants.Exceptions.FrameTimeout) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.FrameTimeout, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the capture target is closed (e.g. <c>GraphicsCaptureItem.Closed</c>
/// or the window HWND is no longer valid) while waiting for a frame.
/// </summary>
public sealed class CaptureTargetClosedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetClosedException"/> class.
    /// </summary>
    public CaptureTargetClosedException() : base(LocalizationConstants.Exceptions.TargetClosed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.TargetClosed, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the crop rectangle is invalid (negative origin or non-positive size).
/// </summary>
public sealed class CaptureCropInvalidException : LocalizedException
{
    private readonly int _x, _y, _w, _h;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureCropInvalidException"/> class.
    /// </summary>
    public CaptureCropInvalidException(int x, int y, int width, int height)
        : base(LocalizationConstants.Exceptions.CropInvalid)
    {
        _x = x; _y = y; _w = width; _h = height;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.CropInvalid, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _x, _y, _w, _h);
    }
}

/// <summary>
/// Thrown when the crop rectangle exceeds the captured surface bounds.
/// </summary>
public sealed class CaptureCropExceedsBoundsException : LocalizedException
{
    private readonly int _x, _y, _w, _h, _srcW, _srcH;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureCropExceedsBoundsException"/> class.
    /// </summary>
    public CaptureCropExceedsBoundsException(int x, int y, int width, int height, int sourceWidth, int sourceHeight)
        : base(LocalizationConstants.Exceptions.CropExceedsBounds)
    {
        _x = x; _y = y; _w = width; _h = height; _srcW = sourceWidth; _srcH = sourceHeight;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.CropExceedsBounds, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _x, _y, _w, _h, _srcW, _srcH);
    }
}

/// <summary>
/// Thrown when the requested pixel format cannot be produced by the capture pipeline.
/// </summary>
public sealed class CapturePixelFormatNotSupportedException : LocalizedException
{
    private readonly string _formatName;
    private readonly int _bytesPerPixel;

    /// <summary>
    /// Gets the pixel format name.
    /// </summary>
    public string FormatName => _formatName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapturePixelFormatNotSupportedException"/> class.
    /// </summary>
    public CapturePixelFormatNotSupportedException(string formatName, int bytesPerPixel)
        : base(LocalizationConstants.Exceptions.PixelFormatNotSupported)
    {
        _formatName = formatName;
        _bytesPerPixel = bytesPerPixel;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.PixelFormatNotSupported, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _formatName, _bytesPerPixel);
    }
}

/// <summary>
/// Thrown when Direct3D / WinRT device setup fails.
/// </summary>
public sealed class CaptureDeviceException : LocalizedException
{
    private readonly string _detail;

    /// <summary>
    /// Gets the technical failure detail.
    /// </summary>
    public string Detail => _detail;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureDeviceException"/> class.
    /// </summary>
    /// <param name="detail">Technical detail (API name and/or HRESULT).</param>
    public CaptureDeviceException(string detail) : base(LocalizationConstants.Exceptions.DeviceFailed)
    {
        _detail = detail;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.DeviceFailed, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _detail);
    }
}
