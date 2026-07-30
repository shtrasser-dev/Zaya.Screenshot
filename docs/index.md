# Zaya.Screenshot

High-performance screen capture for Windows .NET 8.0+ — Windows Graphics Capture + Direct3D 11, with `IRawImage` / `ReadOnlySpan<byte>` pixel access.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.Screenshot** | 1.0.0 | Abstractions: `ICaptureService`, `ICaptureSession`, region types |
| **Zaya.Screenshot.Impl.Windows** | 1.0.0.0 | Windows Graphics Capture + D3D11 (`CaptureService`) |

## Features

- Capture full desktops, individual windows, or rectangular sub-regions
- Pixel formats: BGRA32, RGB24, BGR24, Gray8
- High-performance `ReadOnlySpan<byte>` access via `IRawImage`
- Optional helpers mapping `PixelFormat` to SkiaSharp / ImageSharp type names

There is no separate `InitializeAsync`: create a session with `CreateSessionAsync` and call `CaptureAsync`.

## Installation

```xml
<PackageReference Include="Zaya.Screenshot" Version="1.0.0" />
<PackageReference Include="Zaya.Screenshot.Impl.Windows" Version="1.0.0.0" />
```

## Platform

- Windows 10 version 19041 (20H1) or later
- Direct3D 11 compatible GPU with BGRA support

## Quick Start

```csharp
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

using var service = new CaptureService();

var region = new FullScreenDesktopRegion();
using var session = await service.CreateSessionAsync(region);
using var frame = await session.CaptureAsync();

if (frame is null)
    return;

var pixelData = frame.GetPixelData();
Console.WriteLine($"Captured {frame.Width}x{frame.Height}, format: {frame.Format.Name}");

byte[] copy = frame.ToByteArray();
```

## Next Steps

- **[Getting Started](articles/getting-started.md)** — detailed usage guide and capture scenarios
- **[API Reference](xref:Zaya.Screenshot.Services)** — complete API documentation generated from source code
