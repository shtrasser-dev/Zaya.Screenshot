# Zaya.Screenshot

High-performance screen capture library for Windows .NET 8.0+ applications. Captures windows and monitors using Direct3D 11 with efficient `ReadOnlySpan<byte>` pixel data access and configurable output formats.

## Features

- Capture full desktops, individual windows, or rectangular sub-regions
- Multiple pixel formats: BGRA32, RGB24, BGR24, Gray8
- High-performance `ReadOnlySpan<byte>` access to pixel data
- Frame-level pause/resume control
- Built-in SkiaSharp and ImageSharp format conversion helpers

## Installation

```xml
<PackageReference Include="Zaya.Screenshot" Version="0.2.0" />
```

## Platform

- Windows 10 version 19041 (20H1) or later
- Direct3D 11 compatible GPU with BGRA support

## Quick Start

```csharp
using Zaya.Screenshot.Impl.Windows.Services.Impl;
using Zaya.Screenshot.Models;

using var service = new CaptureService();

// Capture entire primary monitor
var region = new FullScreenDesktopRegion();
using var session = await service.CreateSessionAsync(region);
using var frame = await session.CaptureAsync();

var pixelData = frame.GetPixelData();
Console.WriteLine($"Captured {frame.Width}x{frame.Height}, format: {frame.Format.Name}");

// Copy to byte array for persistence
byte[] copy = frame.ToByteArray();
```

## Next Steps

- **Getting Started** — detailed usage guide and capture scenarios
- **API Reference** — complete API documentation generated from source code
