using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Zaya.Primitives;
using Zaya.Screenshot.Impl.Windows.Models;
using Zaya.Screenshot.Impl.Windows.Services.Impl.WinApi;
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
    private readonly GraphicsCaptureItem _captureItem;
    private readonly Direct3D11CaptureFramePool _framePool;
    private readonly GraphicsCaptureSession _session;
    private readonly Action? _onDisposed;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly nint _windowHandle;

    private SizeInt32 _lastSize;
    private bool _disposed;
    private volatile bool _captureTargetClosed;

    public ICaptureRegion Region => _region;

    public CaptureSession(
        Direct3DConverterService converter,
        ICaptureRegion region,
        GraphicsCaptureItem captureItem,
        Direct3D11CaptureFramePool framePool,
        GraphicsCaptureSession session,
        SizeInt32 initialSize,
        Action? onDisposed = null)
    {
        _converter = converter;
        _region = region;
        _captureItem = captureItem;
        _framePool = framePool;
        _session = session;
        _lastSize = initialSize;
        _onDisposed = onDisposed;
        _windowHandle = region switch
        {
            FullScreenWindowRegion windowRegion => windowRegion.WindowHandle,
            RectWindowRegion windowRegion => windowRegion.WindowHandle,
            _ => 0
        };

        _captureItem.Closed += OnCaptureItemClosed;

        // Window may already be gone between item creation and session start.
        if (_windowHandle != 0 && !WinApiInterop.IsWindow(_windowHandle))
            SignalCaptureTargetClosed();
    }

    public async Task<IRawImage?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfCaptureTargetClosed();

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
            ThrowIfCaptureTargetClosed();

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

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCts.Token);

        _framePool.FrameArrived += OnFrameArrived;
        try
        {
            var existingFrame = _framePool.TryGetNextFrame();
            if (existingFrame != null)
            {
                result = existingFrame;
                return result;
            }

            try
            {
                result = await tcs.Task.WaitAsync(linkedCts.Token);
                return result;
            }
            catch (OperationCanceledException) when (_captureTargetClosed)
            {
                throw new CaptureTargetClosedException();
            }
            catch (OperationCanceledException) when (_disposed)
            {
                throw new ObjectDisposedException(nameof(CaptureSession));
            }
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

    private void OnCaptureItemClosed(GraphicsCaptureItem sender, object args)
        => SignalCaptureTargetClosed();

    private void SignalCaptureTargetClosed()
    {
        _captureTargetClosed = true;
        try
        {
            _lifetimeCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void ThrowIfCaptureTargetClosed()
    {
        if (_captureTargetClosed)
            throw new CaptureTargetClosedException();

        if (_windowHandle != 0 && !WinApiInterop.IsWindow(_windowHandle))
        {
            SignalCaptureTargetClosed();
            throw new CaptureTargetClosedException();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _captureItem.Closed -= OnCaptureItemClosed; } catch { }

        try { _lifetimeCts.Cancel(); } catch { }
        try { _lifetimeCts.Dispose(); } catch { }

        try { _session.Dispose(); } catch { }
        try { _framePool.Dispose(); } catch { }
        _onDisposed?.Invoke();
    }
}
