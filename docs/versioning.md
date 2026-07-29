# Versioning (Zaya.Screenshot)

Aligned with the ScreenTranslator updater plan and **Zaya.Primitives** compatibility channel.

## Versions

| Artifact | Rule | Example |
|----------|------|---------|
| `Zaya.Primitives` | `MAJOR.MINOR.0` | `0.4.0` |
| This repo (`Directory.Build.props`) | Same `MAJOR.MINOR`, own `PATCH` | `0.4.0` |
| GitHub Release (immutable) | `plugin-v{MAJOR.MINOR.PATCH}` | `plugin-v0.4.0` |
| GitHub Release (floating) | `plugin-v{MAJOR.MINOR}-latest` | `plugin-v0.4-latest` |

Both NuGet packages and the Windows plugin zip use the same `Version` from [`Directory.Build.props`](../Directory.Build.props).

## Plugin zip

- **Stable name:** `Zaya.Screenshot.Impl.Windows.zip` (no version in the filename).
- Contains `plugin.json`:

```json
{
  "id": "GraphicsCapture",
  "type": "capture",
  "interface": "Zaya.Screenshot",
  "interfaceVersion": "0.4.0",
  "pluginVersion": "0.4.0",
  "primitivesChannel": "0.4"
}
```

Hosts discover updates via:

`GET /repos/shtrasser-dev/Zaya.Screenshot/releases/tags/plugin-v0.4-latest`

Release `name` is `Plugin v0.4.0` (semver for comparison without downloading the zip).

## Bumping

1. Edit `Version` / `ZayaPrimitivesVersion` in `Directory.Build.props`.
2. Run `build.cmd` (or publish workflow).
3. CI creates `plugin-v{ver}` and refreshes `plugin-v{channel}-latest`.
