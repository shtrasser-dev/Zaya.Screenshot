# Zaya.Screenshot

High-performance screen capture for Windows .NET 8.0+ — Windows Graphics Capture + Direct3D 11, with `IRawImage` / `ReadOnlySpan<byte>` pixel access and configurable output formats.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.Screenshot** | 1.1.0 | Abstractions: `ICaptureService`, `ICaptureSession`, region types, `PixelFormatExtensions` |
| **Zaya.Screenshot.Impl.Windows** | 1.1.0.0 | Windows Graphics Capture + D3D11 (`CaptureService`, `CapturedFrame`) |

Requires [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives) **1.0.0**.

Update channel (GitHub Releases): [`plugin-Zaya.Screenshot-v1.1-latest`](https://github.com/shtrasser-dev/Zaya.Screenshot/releases/tag/plugin-Zaya.Screenshot-v1.1-latest)

See [versioning](docs/versioning.md).

Docs: [API & articles](https://shtrasser-dev.github.io/Zaya.Screenshot)

## Features

- Capture full desktops, individual windows, or rectangular sub-regions (`FullScreenDesktopRegion`, `FullScreenWindowRegion`, `RectDesktopRegion`, `RectWindowRegion`)
- Pixel formats: BGRA32, RGB24, BGR24, Gray8
- High-performance `ReadOnlySpan<byte>` access via `IRawImage`
- Engine metadata (`EngineId`, localized name/description, `Settings`) for plugin hosts
- Windows engine failures surface as `LocalizedException` for host UI
- UI/error strings for the Windows engine: `en`, `ru`, `zh-Hans`, `uk`, `de`, `pt`, `ja`, `ko`, `fr`, `tr`, `pl`
- Optional helpers mapping `PixelFormat` to SkiaSharp / ImageSharp type names

There is no separate `InitializeAsync` / pause-resume API: create a session and call `CaptureAsync`.

## Platform

- Windows 10 version 19041 (20H1) or later
- Direct3D 11 compatible GPU with BGRA support

## Installation

```xml
<PackageReference Include="Zaya.Screenshot" Version="1.1.0" />
<PackageReference Include="Zaya.Screenshot.Impl.Windows" Version="1.1.0.0" />
```

Plugin zip for ScreenTranslator hosts: `Zaya.Screenshot.Impl.Windows.zip` from the floating tag above.

## Quick Start

```csharp
using System.Globalization;
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

using var service = new CaptureService();

Console.WriteLine(service.DisplayName.GetValue(CultureInfo.CurrentUICulture));
Console.WriteLine(service.IsAvailable);

// Capture entire primary monitor
var region = new FullScreenDesktopRegion();
using var session = await service.CreateSessionAsync(region);
using var frame = await session.CaptureAsync();

if (frame is null)
    return;

var pixelData = frame.GetPixelData();
Console.WriteLine($"Captured {frame.Width}x{frame.Height}, format: {frame.Format.Name}");

byte[] copy = frame.ToByteArray();
```

### Capture a Window

```csharp
var region = new FullScreenWindowRegion { WindowHandle = hwnd };
using var session = await service.CreateSessionAsync(region);
```

### Capture a Rectangular Sub-Region

```csharp
var region = new RectDesktopRegion
{
    DisplayIndex = 0,
    // Coordinates relative to the top-left of that display (not the virtual desktop)
    Rectangle = new Rectangle(100, 100, 400, 300)
};
```

### Select Pixel Format

```csharp
var region = new FullScreenDesktopRegion
{
    PixelFormat = PixelFormat.Gray8 // 1 byte per pixel, useful for OCR
};
```

## Architecture

```
Resolve ICaptureService (new / DI / plugin host)
  → Read DisplayName / Description / Settings / IsAvailable
  → CreateSessionAsync(region[, settings])
  → CaptureAsync() → IRawImage
  → Dispose session / service
```

- **Zaya.Screenshot** — interfaces and region models
- **Zaya.Screenshot.Impl.Windows** — Windows Graphics Capture + Direct3D 11

## License

MIT — see [LICENSE](LICENSE).
