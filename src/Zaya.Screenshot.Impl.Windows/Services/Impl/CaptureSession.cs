using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Models;
using Zaya.Screenshot.Models;
using Zaya.Screenshot.Services;

namespace Zaya.Screenshot.Impl.Windows.Services.Impl;

/// <summary>
/// Implementation of <see cref="ICaptureSession"/>.
/// </summary>
internal sealed class CaptureSession : ICaptureSession
{
    private const int FramePoolBufferCount = 2;

    private readonly Direct3DConverterService _converter;
    private readonly ICaptureRegion _region;
    private readonly Direct3D11CaptureFramePool _framePool;
    private readonly GraphicsCaptureSession _session;
    private readonly Action? _onDisposed;

    private SizeInt32 _lastSize;
    private bool _disposed;

    public ICaptureRegion Region => _region;

    public CaptureSession(
        Direct3DConverterService converter,
        ICaptureRegion region,
        Direct3D11CaptureFramePool framePool,
        GraphicsCaptureSession session,
        SizeInt32 initialSize,
        Action? onDisposed = null)
    {
        _converter = converter;
        _region = region;
        _framePool = framePool;
        _session = session;
        _lastSize = initialSize;
        _onDisposed = onDisposed;
    }

    public async Task<IRawImage?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = await WaitForUsableFrameAsync(cancellationToken);
        if (frame == null)
            return null;

        try
        {
            return await ConvertFrameAsync(frame);
        }
        finally
        {
            frame.Dispose();
        }
    }

    private async Task<IRawImage> ConvertFrameAsync(Direct3D11CaptureFrame frame)
    {
        PixelFormat outputFormat = _region.PixelFormat;

        var (pixelData, width, height) = await _converter.ConvertSurfaceToByteArrayAsync(
            frame.Surface,
            _region);

        int bytesPerPixel = outputFormat.BytesPerPixel;
        int stride = width * bytesPerPixel;

        return new CapturedFrame(
            pixelData,
            width,
            height,
            stride,
            outputFormat);
    }

    /// <summary>
    /// Waits for a frame whose <see cref="Direct3D11CaptureFrame.ContentSize"/> matches the
    /// current frame-pool buffers. When the captured window is restored or resized, WGC
    /// reports a new content size while the pool still has the old buffers — recreate and
    /// wait for the next frame (Microsoft SimpleCapture pattern).
    /// </summary>
    private async Task<Direct3D11CaptureFrame?> WaitForUsableFrameAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);

            var frame = await WaitForNextFrameAsync(cancellationToken);
            if (frame is null)
                return null;

            var contentSize = frame.ContentSize;

            // Minimized / empty content — discard and keep waiting (do not Recreate with 0).
            if (contentSize.Width <= 0 || contentSize.Height <= 0)
            {
                frame.Dispose();
                continue;
            }

            if (contentSize.Width == _lastSize.Width && contentSize.Height == _lastSize.Height)
                return frame;

            // Size changed: drop this frame, resize the pool, wait for a matching one.
            frame.Dispose();
            _framePool.Recreate(
                _converter.WinRTDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                FramePoolBufferCount,
                contentSize);
            _lastSize = contentSize;
        }
    }

    private async Task<Direct3D11CaptureFrame?> WaitForNextFrameAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<Direct3D11CaptureFrame?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Direct3D11CaptureFrame? result = null;

        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            try
            {
                var frame = sender.TryGetNextFrame();
                if (frame is null)
                    return;

                if (!tcs.TrySetResult(frame))
                    frame.Dispose();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        _framePool.FrameArrived += OnFrameArrived;
        try
        {
            var existingFrame = _framePool.TryGetNextFrame();
            if (existingFrame != null)
            {
                result = existingFrame;
                return result;
            }

            result = await tcs.Task.WaitAsync(cancellationToken);
            return result;
        }
        finally
        {
            _framePool.FrameArrived -= OnFrameArrived;

            if (tcs.Task.IsCompletedSuccessfully)
            {
                var leftover = tcs.Task.Result;
                if (leftover is not null && !ReferenceEquals(leftover, result))
                    leftover.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _session.Dispose(); } catch { }
        try { _framePool.Dispose(); } catch { }
        _onDisposed?.Invoke();
    }
}
