# Versioning (Zaya.Screenshot)

| Axis | Source | Example |
|------|--------|---------|
| **ZayaPrimitivesVersion** | `Directory.Build.props` (supplies **Major**) | `1.0.0` |
| **interfaceVersion** | `Zaya.Screenshot.csproj` → only **`ZayaVersionInterface`** → `Major.Interface.0` | `1.0.0` |
| **pluginVersion** | Impl → **`ZayaVersionImpMajor`** + **`ZayaVersionImpMinor`**; Interface read from abstractions csproj → `Major.Interface.ImpMajor.ImpMinor` | `1.0.0.0` |
| **updateChannel** | Interface `MAJOR.Interface` | `1.0` → `plugin-v1.0-latest` |

Rules:

- Abstractions: only `ZayaVersionInterface`. Version always ends with `.0`. Contract/assembly change → bump Interface.
- Plugin: only `ZayaVersionImpMajor` / `ZayaVersionImpMinor` (4th segment allowed). Interface digit is taken from abstractions automatically.
- Do not set `<Version>` manually. `Directory.Build.targets` builds it and checks Major vs Primitives.
- Host loads a zip only if `interfaceVersion` **exactly** matches host’s `Zaya.Screenshot` version.
- Updater uses `plugin-v{updateChannel}-latest` (not Primitives).

## plugin.json

```json
{
  "id": "GraphicsCapture",
  "type": "capture",
  "interface": "Zaya.Screenshot",
  "interfaceVersion": "1.0.0",
  "pluginVersion": "1.0.0.0"
}
```

## Bumping

1. Interface: raise `ZayaVersionInterface` in `Zaya.Screenshot.csproj`, update host, republish plugins.
2. Plugin only: raise `ZayaVersionImpMajor` and/or `ZayaVersionImpMinor` in Impl csproj.
3. Run `build.cmd` / Publish workflow.
