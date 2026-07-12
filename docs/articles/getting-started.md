# Getting Started

## Basic Capture Scenarios

### Capture Entire Primary Monitor

```csharp
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

## Pause and Resume

```csharp
session.Pause();
// ... capture resumes when needed
session.Resume();
```

## Cleanup

Both `CaptureService` and `ICaptureSession` implement `IDisposable`. Each captured frame is also disposable — returned to the shared array pool on dispose for memory efficiency.
