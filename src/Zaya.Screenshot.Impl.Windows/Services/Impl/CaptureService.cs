using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using WinRT;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Constants;
using Zaya.Screenshot.Impl.Windows.Services.Impl.WinApi;
using Zaya.Screenshot.Models;
using Zaya.Screenshot.Services;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

/// <summary>
/// Implementation of <see cref="ICaptureService"/> using Windows Graphics Capture API and Direct3D 11.
/// Supports capturing windows and monitors in full-screen or rectangular regions.
/// Pass engine settings directly to <see cref="CreateSessionAsync(ICaptureRegion, IReadOnlyDictionary{string, object}, CancellationToken)"/>.
/// </summary>
public sealed class CaptureService : ICaptureService
{
    private Direct3DConverterService? _converter;
    private int _activeSessions;
    private bool _disposed;

    private static readonly IReadOnlyList<SettingDescriptor> SettingsList = [];

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);

    /// <inheritdoc />
    public string EngineId => "graphics-capture";

    /// <inheritdoc />
    public LocalizedString DisplayName => Loc(LocalizationConstants.Engine.Name);

    /// <inheritdoc />
    public LocalizedString Description => Loc(LocalizationConstants.Engine.Desc);

    /// <inheritdoc />
    public IReadOnlyList<SettingDescriptor> Settings { get; } = SettingsList;

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureService"/> class.
    /// The constructor is lightweight — Direct3D initialization is deferred to <see cref="CreateSessionAsync(ICaptureRegion, IReadOnlyDictionary{string, object}, CancellationToken)"/>.
    /// </summary>
    public CaptureService()
    {
    }

    /// <inheritdoc />
    public async Task<ICaptureSession> CreateSessionAsync(
        ICaptureRegion region,
        CancellationToken cancellationToken = default)
    {
        var settingDescriptorList = new SettingDescriptorList(SettingsList);
        return await CreateSessionAsync(region, settingDescriptorList, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ICaptureSession> CreateSessionAsync(
        ICaptureRegion region,
        IReadOnlyDictionary<string, object> engineSettings,
        CancellationToken cancellationToken = default)
    {
        var settingDescriptorList = new SettingDescriptorList(SettingsList);
        settingDescriptorList.Bind(engineSettings);
        return await CreateSessionAsync(region, settingDescriptorList, cancellationToken);
    }

    private async Task<ICaptureSession> CreateSessionAsync(
        ICaptureRegion region,
        SettingDescriptorList settingDescriptorList,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(region);
        cancellationToken.ThrowIfCancellationRequested();

        if (_converter is null)
        {
            if (!GraphicsCaptureSession.IsSupported())
                throw new CaptureNotSupportedException();

            _converter = Direct3DConverterService.Create();
        }

        var (captureItem, captureSize, isMonitorCapture) = CreateCaptureItem(region);

        if (captureItem == null)
            throw new CaptureItemCreateException();

        Direct3D11CaptureFramePool? framePool = null;
        GraphicsCaptureSession? session = null;
        CaptureSession? captureSession = null;
        try
        {
            framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _converter.WinRTDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                captureSize);

            session = framePool.CreateCaptureSession(captureItem);
            try { session.IsCursorCaptureEnabled = false; } catch { }

            session.StartCapture();

            Interlocked.Increment(ref _activeSessions);
            captureSession = new CaptureSession(
                _converter,
                region,
                framePool,
                session,
                OnSessionDisposed);

            // Ownership transferred to CaptureSession.
            framePool = null;
            session = null;

            if (isMonitorCapture)
            {
                for (var i = 0; i < 3; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var frame = await captureSession.CaptureAsync(cancellationToken);
                    frame?.Dispose();
                }
            }

            return captureSession;
        }
        catch
        {
            captureSession?.Dispose();
            try { session?.Dispose(); } catch { }
            try { framePool?.Dispose(); } catch { }
            throw;
        }
    }

    private void OnSessionDisposed()
    {
        if (Interlocked.Decrement(ref _activeSessions) == 0 && _disposed)
            _converter?.Dispose();
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
                if (windowItem is null)
                    throw new CaptureWindowItemCreateException(hwnd);
                captureSize = GetWindowCaptureSize(hwnd, windowItem);
                return (windowItem, captureSize, false);

            case RectWindowRegion windowRegion:
                hwnd = windowRegion.WindowHandle;
                var rectWindowItem = CreateForWindow(hwnd);
                if (rectWindowItem is null)
                    throw new CaptureWindowItemCreateException(hwnd);
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
                throw new CaptureRegionNotSupportedException(region.GetType().Name);
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
            throw new CaptureDisplayIndexException(displayIndex, monitors.Length);

        return monitors[displayIndex];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (Volatile.Read(ref _activeSessions) == 0)
            _converter?.Dispose();
    }
}
