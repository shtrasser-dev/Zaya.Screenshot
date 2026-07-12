using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl.WinApi;

internal static class WinApiInterop
{
    private const int MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfoW(nint hMonitor, ref MONITORINFO lpmi);

    public static nint[] GetMonitorHandles()
    {
        var handles = new System.Collections.Generic.List<nint>();
        EnumDisplayMonitors(nint.Zero, nint.Zero,
            (nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData) =>
            {
                handles.Add(hMonitor);
                return true;
            }, nint.Zero);
        return handles.ToArray();
    }

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(nint hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [DllImport("combase.dll")]
    public static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        int length, out IntPtr hstring);

    [DllImport("combase.dll")]
    public static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll")]
    public static extern int RoGetActivationFactory(
        IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("d3d11.dll")]
    public static extern int D3D11CreateDevice(
        IntPtr pAdapter, int driverType, IntPtr software, uint flags,
        IntPtr featureLevels, uint featureLevelCount, uint sdkVersion,
        out IntPtr device, out int featureLevel, out IntPtr context);

    [DllImport("d3d11.dll")]
    public static extern int CreateDirect3D11DeviceFromDXGIDevice(
        IntPtr dxgiDevice, out IntPtr graphicsDevice);

    public static GraphicsCaptureItem? CreateCaptureItemForWindow(nint hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
        int hr = WindowsCreateString(className, className.Length, out IntPtr hClassName);
        if (hr != 0)
            return null;

        try
        {
            Guid interopGuid = typeof(IGraphicsCaptureItemInterop).GUID;
            hr = RoGetActivationFactory(hClassName, ref interopGuid, out IntPtr factoryPtr);
            if (hr != 0)
                return null;

            try
            {
                var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
                Guid itemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

                IntPtr itemPtr = interop.CreateForWindow(hwnd, ref itemIid);
                if (itemPtr == IntPtr.Zero)
                {
                    nint monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                    itemPtr = interop.CreateForMonitor(monitor, ref itemIid);
                }
                if (itemPtr == IntPtr.Zero)
                    return null;

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
            WindowsDeleteString(hClassName);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}
