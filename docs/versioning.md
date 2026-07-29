# Versioning (Zaya.Screenshot)

Three independent axes — do not bump them together unless required.

| Axis | Source | Example |
|------|--------|---------|
| **primitivesChannel** | `ZayaPrimitivesVersion` → `MAJOR.MINOR` | `0.4` |
| **interfaceVersion** | Version of package **Zaya.Screenshot** | `0.4.0` |
| **pluginVersion** | Version of **Impl.Windows** | `0.4.0` |

Host must ship the same **Zaya.Screenshot** assembly as `interfaceVersion`. Capture-only fixes: bump only the Impl `<Version>`.

Release body lists `Zaya.Screenshot.Impl.Windows.zip=0.4.0` for the host updater. Floating tag: `plugin-v{channel}-latest`.
