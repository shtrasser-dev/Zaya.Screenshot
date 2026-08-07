# Versioning (Zaya.Screenshot)

| Axis | Source | Example |
|------|--------|---------|
| **ZayaPrimitivesVersion** | `Directory.Build.props` (supplies **Major**) | `1.0.0` |
| **interfaceVersion** | `Zaya.Screenshot.csproj` → only **`ZayaVersionInterface`** → `Major.Interface.0` | `1.1.0` |
| **pluginVersion** | Impl → **`ZayaVersionImpMajor`** + **`ZayaVersionImpMinor`**; Interface read from abstractions csproj → `Major.Interface.ImpMajor.ImpMinor` | `1.1.0.0` |
| **updateChannel** | Interface `MAJOR.Interface` | `1.1` → `plugin-Zaya.Screenshot-v1.1-latest` |

Rules:

- Abstractions: only `ZayaVersionInterface`. Version always ends with `.0`. Contract/assembly change → bump Interface.
- Plugin: only `ZayaVersionImpMajor` / `ZayaVersionImpMinor` (4th segment allowed). Interface digit is taken from abstractions automatically.
- Do not set `<Version>` manually. `Directory.Build.targets` builds it and checks Major vs Primitives.
- Host loads a zip only if `interfaceVersion` **exactly** matches host’s `Zaya.Screenshot` version.
- **One interface → one floating GitHub tag:** `plugin-Zaya.Screenshot-v{channel}-latest` (immutable: `plugin-Zaya.Screenshot-v{pluginVersion}`).
- `build.cmd` writes `out/interfaces.json` for the Publish workflow.

## plugin.json

```json
{
  "id": "GraphicsCapture",
  "type": "capture",
  "interface": "Zaya.Screenshot",
  "interfaceVersion": "1.1.0",
  "pluginVersion": "1.1.0.0"
}
```

## Bumping

1. Interface: raise `ZayaVersionInterface` in `Zaya.Screenshot.csproj`, update host, republish plugins.
2. Plugin only: raise `ZayaVersionImpMajor` and/or `ZayaVersionImpMinor` in Impl csproj.
3. Run `build.cmd` / Publish workflow.
