using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using WinRT;
using Zaya.Screenshot.Impl.Windows.Services.Impl.WinApi;
using Zaya.Screenshot.Models;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

internal sealed class Direct3DConverterService : IDisposable
{
    private IntPtr _d3dDevice;
    private IntPtr _d3dContext;
    private IDirect3DDevice? _winrtDevice;
    private readonly FrameCropperService _cropper;
    private bool _disposed;

    public IDirect3DDevice WinRTDevice =>
        _winrtDevice ?? throw new ObjectDisposedException(nameof(Direct3DConverterService));

    private Direct3DConverterService(IntPtr d3dDevice, IntPtr d3dContext, IDirect3DDevice winrtDevice, FrameCropperService cropper)
    {
        _d3dDevice = d3dDevice;
        _d3dContext = d3dContext;
        _winrtDevice = winrtDevice;
        _cropper = cropper;
    }

    public static Direct3DConverterService Create()
    {
        int hr = WinApiInterop.D3D11CreateDevice(
            IntPtr.Zero, D3D_DRIVER_TYPE_HARDWARE, IntPtr.Zero,
            D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            IntPtr.Zero, 0, D3D11_SDK_VERSION,
            out IntPtr d3dDevice, out _, out IntPtr d3dContext);

        if (hr != 0)
            throw new COMException($"Failed to create D3D11 device (hr=0x{hr:X})", hr);

        try
        {
            Guid dxgiGuid = IID_IDXGIDevice;
            hr = Marshal.QueryInterface(d3dDevice, ref dxgiGuid, out IntPtr dxgiDevice);
            if (hr != 0)
                throw new COMException("Failed to get IDXGIDevice", hr);

            try
            {
                hr = WinApiInterop.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out IntPtr inspectable);
                if (hr != 0)
                    throw new COMException("Failed to create WinRT Direct3D device", hr);

                try
                {
                    var winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
                    return new Direct3DConverterService(d3dDevice, d3dContext, winrtDevice, new FrameCropperService());
                }
                finally
                {
                    Marshal.Release(inspectable);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        catch
        {
            Marshal.Release(d3dContext);
            Marshal.Release(d3dDevice);
            throw;
        }
    }

    public async Task<(byte[] Data, int Width, int Height)> ConvertSurfaceToByteArrayAsync(
        IDirect3DSurface surface,
        ICaptureRegion region)
    {
        if (surface == null)
            throw new ArgumentNullException(nameof(surface));

        var desc = surface.Description;
        int srcWidth = desc.Width;
        int srcHeight = desc.Height;

        var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(surface);

        try
        {
            int srcTotalBytes = srcWidth * srcHeight * 4;
            var buffer = new Buffer((uint)srcTotalBytes);
            softwareBitmap.CopyToBuffer(buffer);

            var data = _cropper.Crop(buffer, srcWidth, srcHeight, region, out int width, out int height);
            return (data, width, height);
        }
        finally
        {
            softwareBitmap.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _winrtDevice?.Dispose();
        _winrtDevice = null;

        if (_d3dContext != IntPtr.Zero) { Marshal.Release(_d3dContext); _d3dContext = IntPtr.Zero; }
        if (_d3dDevice != IntPtr.Zero) { Marshal.Release(_d3dDevice); _d3dDevice = IntPtr.Zero; }
    }

    private const int D3D_DRIVER_TYPE_HARDWARE = 1;
    private const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
    private const uint D3D11_SDK_VERSION = 7;

    private static readonly Guid IID_IDXGIDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");

}
