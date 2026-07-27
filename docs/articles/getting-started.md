# Getting Started

## Overview

Zaya.Screenshot provides screen-capture abstractions (`ICaptureService`, `ICaptureSession`, `ICaptureRegion`) and a Windows implementation based on Graphics Capture + Direct3D 11. Frames are returned as `IRawImage` (`ReadOnlySpan<byte>` pixel data).

## Basic Capture Scenarios

### Capture Entire Primary Monitor

```csharp
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

using var service = new CaptureService();
var region = new FullScreenDesktopRegion();
using var session = await service.CreateSessionAsync(region);
using var frame = await session.CaptureAsync();
```

### Capture a Specific Monitor

```csharp
var region = new FullScreenDesktopRegion { DisplayIndex = 1 };
```

### Capture a Window

```csharp
nint hwnd = GetWindowHandle(); // Your window handle
var region = new FullScreenWindowRegion { WindowHandle = hwnd };
```

### Capture a Rectangular Sub-Region

```csharp
var region = new RectDesktopRegion
{
    DisplayIndex = 0,
    // Origin is the top-left of the selected display, not the virtual screen
    Rectangle = new Rectangle(100, 100, 400, 300)
};
```

## Selecting Pixel Format

```csharp
var region = new FullScreenDesktopRegion
{
    PixelFormat = PixelFormat.Gray8 // Single-channel, 1 byte per pixel
};
```

Available formats: `Bgra32` (default), `Rgb24`, `Bgr24`, `Gray8`.

## Cancellation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
using var session = await service.CreateSessionAsync(region, cts.Token);
using var frame = await session.CaptureAsync(cts.Token);
```

## Cleanup

`ICaptureService`, `ICaptureSession`, and each captured `IRawImage` implement `IDisposable`. Dispose frames promptly so buffers can return to the shared array pool. Dispose sessions before disposing the service (the service owns the shared Direct3D device).

## Next steps

- **[API Reference](xref:Zaya.Screenshot.Services)** — generated from source
