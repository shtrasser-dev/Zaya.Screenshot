using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using WinRT;
using Zaya.Screenshot.Impl.Windows.Services.Impl.WinApi;
using Zaya.Screenshot.Models;
using Zaya.Screenshot.Services;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

/// <summary>
/// Implementation of <see cref="ICaptureService"/> using Windows Graphics Capture API and Direct3D 11.
/// Supports capturing windows and monitors in full-screen or rectangular regions.
/// </summary>
public sealed class CaptureService : ICaptureService
{
    private readonly Direct3DConverterService _converter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureService"/> class.
    /// Creates a Direct3D 11 device and converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when Windows Graphics Capture is not supported on this system.</exception>
    /// <exception cref="COMException">Thrown when the Direct3D device cannot be created.</exception>
    public CaptureService()
    {
        if (!GraphicsCaptureSession.IsSupported())
            throw new NotSupportedException("Windows Graphics Capture is not supported on this system.");

        _converter = Direct3DConverterService.Create();
    }

    /// <summary>
    /// Creates a new capture session for the specified region.
    /// For monitor captures, the first few frames are discarded to avoid initial black frames.
    /// </summary>
    /// <param name="region">The region to capture. Must be one of <see cref="FullScreenWindowRegion"/>,
    /// <see cref="RectWindowRegion"/>, <see cref="FullScreenDesktopRegion"/>, or <see cref="RectDesktopRegion"/>.</param>
    /// <param name="cancellationToken">Token to cancel the session creation.</param>
    /// <returns>An active capture session ready to produce frames.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="region"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the GraphicsCaptureItem cannot be created.</exception>
    /// <exception cref="NotSupportedException">Thrown when the region type is not recognized.</exception>
    public async Task<ICaptureSession> CreateSessionAsync(
        ICaptureRegion region,
        CancellationToken cancellationToken = default)
    {
        if (region == null)
            throw new ArgumentNullException(nameof(region));

        var (captureItem, captureSize, isMonitorCapture) = CreateCaptureItem(region);

        if (captureItem == null)
            throw new InvalidOperationException("Failed to create GraphicsCaptureItem.");

        // Create frame pool
        var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _converter.WinRTDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            captureSize);

        var session = framePool.CreateCaptureSession(captureItem);
        try { session.IsCursorCaptureEnabled = false; } catch { }

        // Start capture
        session.StartCapture();

        var captureSession = new CaptureSession(
            _converter,
            region,
            framePool,
            session);

        // Wait for first non-black frame (only for monitor capture)
        if (isMonitorCapture)
        {
            for (var i = 0; i < 3; i++)
            {
                var frame = await captureSession.CaptureAsync();
                frame?.Dispose();
            }
        }

        return captureSession;
    }

    private (GraphicsCaptureItem? Item, SizeInt32 Size, bool IsMonitor) CreateCaptureItem(
        ICaptureRegion region)
    {
        nint hwnd;
        SizeInt32 captureSize;

        switch (region)
        {
            case FullScreenWindowRegion windowRegion:
                hwnd = windowRegion.WindowHandle;
                var windowItem = CreateForWindow(hwnd);
                captureSize = GetWindowCaptureSize(hwnd, windowItem);
                return (windowItem, captureSize, false);

            case RectWindowRegion windowRegion:
                hwnd = windowRegion.WindowHandle;
                var rectWindowItem = CreateForWindow(hwnd);
                captureSize = GetWindowCaptureSize(hwnd, rectWindowItem);
                return (rectWindowItem, captureSize, false);

            case FullScreenDesktopRegion desktopRegion:
                hwnd = GetMonitorHandle(desktopRegion.DisplayIndex);
                var desktopItem = CreateForMonitor(hwnd);
                captureSize = GetMonitorCaptureSize(hwnd, desktopItem);
                return (desktopItem, captureSize, true);

            case RectDesktopRegion desktopRegion:
                hwnd = GetMonitorHandle(desktopRegion.DisplayIndex);
                var rectDesktopItem = CreateForMonitor(hwnd);
                captureSize = GetMonitorCaptureSize(hwnd, rectDesktopItem);
                return (rectDesktopItem, captureSize, true);

            default:
                throw new NotSupportedException($"Region type '{region.GetType().Name}' is not supported.");
        }
    }


    private static GraphicsCaptureItem? CreateForWindow(nint hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        return WinApiInterop.CreateCaptureItemForWindow(hwnd);
    }

    private static GraphicsCaptureItem? CreateForMonitor(nint monitor)
    {
        if (monitor == IntPtr.Zero) return null;

        string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
        int hr = WinApiInterop.WindowsCreateString(className, className.Length, out IntPtr hClassName);
        if (hr != 0) return null;

        try
        {
            Guid interopGuid = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
            hr = WinApiInterop.RoGetActivationFactory(hClassName, ref interopGuid, out IntPtr factoryPtr);
            if (hr != 0) return null;

            try
            {
                var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
                Guid itemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
                IntPtr itemPtr = interop.CreateForMonitor(monitor, ref itemIid);
                if (itemPtr == IntPtr.Zero) return null;

                try
                {
                    return (GraphicsCaptureItem)MarshalInspectable<object>.FromAbi(itemPtr);
                }
                finally
                {
                    Marshal.Release(itemPtr);
                }
            }
            finally
            {
                Marshal.Release(factoryPtr);
            }
        }
        finally
        {
            WinApiInterop.WindowsDeleteString(hClassName);
        }
    }

    private SizeInt32 GetWindowCaptureSize(nint hwnd, GraphicsCaptureItem? item)
    {
        var size = item?.Size ?? default;
        if (size.Width > 0 && size.Height > 0) return size;

        WinApiInterop.GetClientRect(hwnd, out var rect);
        return new SizeInt32(rect.Width, rect.Height);
    }

    private SizeInt32 GetMonitorCaptureSize(nint monitor, GraphicsCaptureItem? item)
    {
        var size = item?.Size ?? default;
        if (size.Width > 0 && size.Height > 0) return size;

        var mi = new WinApiInterop.MONITORINFO { cbSize = Marshal.SizeOf<WinApiInterop.MONITORINFO>() };
        WinApiInterop.GetMonitorInfoW(monitor, ref mi);
        return new SizeInt32(mi.rcMonitor.Width, mi.rcMonitor.Height);
    }

    private static nint GetMonitorHandle(int displayIndex)
    {
        var monitors = WinApiInterop.GetMonitorHandles();
        if (monitors.Length == 0)
            return IntPtr.Zero;

        if ((uint)displayIndex >= (uint)monitors.Length)
            throw new ArgumentOutOfRangeException(nameof(displayIndex),
                $"Display index {displayIndex} is out of range. Available monitors: {monitors.Length}.");

        return monitors[displayIndex];
    }

    /// <summary>
    /// Releases the underlying Direct3D resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _converter?.Dispose();
    }
}