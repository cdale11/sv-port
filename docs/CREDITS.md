# Credits, contributions and provenance

## Game and upstream work

- **ConcernedApe** created [Stardew Valley](https://www.stardewvalley.net/). The game
  is separately licensed and must be supplied by its owner.
- **Producdevity** maintains the [mainline PortMaster port](https://github.com/Producdevity/portmaster-stardew-valley-mainline)
  used as this repository’s starting point. Base commit:
  `986097ce2e586abb799c40dfd7386b288fa8ae1b`. Upstream history and notices are retained.
- **Johnny on Flame** contributed the original Stardew Valley PortMaster port and
  mainline-port support, as credited by upstream.
- **MonoGame** provides the framework. This fork builds
  [Producdevity/MonoGame](https://github.com/Producdevity/MonoGame/tree/portmaster-stardew-mainline)
  at `3874df98675039d368e77dca50d9a00ac970537e`, plus this repository’s rumble patch.
- **PortMaster** provides controls, platform integration and the port ecosystem.
- **KNULLI** provides the target firmware. The H700 motor contract comes from
  [knulli-rumble](https://github.com/knulli-cfw/distribution/blob/ab3a6809286b3cff1648fb8c96a0200bc869beb6/board/batocera/allwinner/h700/fsoverlay/usr/bin/knulli-rumble):
  `/sys/class/power_supply/axp2202-battery/moto` accepts binary on/off writes.

## Contributions in this repository

**cdale11** owns the repository, requested the RG34XXSP/KNULLI adaptation, provided
device logs and confirmed gameplay and rumble on v0.1.1.

**OpenAI Codex assistance** was used to implement and document:

- The managed MonoGame-to-KNULLI motor bridge and native watchdog helper.
- RG34XXSP defaults, launcher lifecycle and persistent-save integration.
- Windows/Linux mainline import cleanup and managed-architecture handling.
- Cecil dependency-handle ownership to fix the startup DLL-lock failure.
- Preparation caching, header-only architecture checks, scoped MangoHud filtering
  and startup timing logs.
- Cross-build scripts, regression fixtures, CI, package verification, releases,
  upgrade ZIPs, troubleshooting and documentation cleanup.

These additions adapt the upstream port; they do not represent authorship of
Stardew Valley, MonoGame, KNULLI or upstream’s existing rendering/audio patches.
Automated tests used fixtures rather than retail game files. Hardware reports are
identified separately in [the validation record](HARDWARE-VALIDATION.md).

## Bundled components and licenses

| Component | Version | Source |
| --- | --- | --- |
| .NET game runtime | 6.0.36, Linux ARM64 | Microsoft .NET |
| SMAPI | 4.5.2 | [Pathoschild/SMAPI](https://github.com/Pathoschild/SMAPI) |
| SkiaSharp native assets | 2.80.3 | SkiaSharp NuGet package |
| GL4ES libraries and native shims | Inherited/adapted from the base port | Base port and its retained notices |
| Mono.Cecil | 0.11.6 | Importer dependency |

The repository’s [MIT license](../LICENSE) retains upstream’s copyright notice.
Additional component notices are retained in the package’s `licenses/` directory
and the .NET runtime tree. AI assistance is disclosed as a development credit,
not as a replacement for those notices.
