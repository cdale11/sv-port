# Contributing to sv-port

This repository maintains a Stardew Valley mainline PortMaster adaptation for
Anbernic RG34XXSP on KNULLI. Use this repository’s issues and pull requests for
fork-specific changes. Before proposing a PortMaster-wide contribution, consult
the upstream PortMaster contribution process and coordinate with its maintainers.

## Reporting a problem

Include the port release, KNULLI version, game version and Windows/Linux source,
mods, reproduction steps and relevant `log.txt` output. For performance reports,
record the same save/location and display mode, separate first and repeat launches,
and state whether MangoHud or profiling was enabled.

Do not commit retail game files, saves, private logs or third-party mod downloads.

## Development

1. Read the [build guide](build/README.md) and [credits/provenance](docs/CREDITS.md).
2. Make a focused change and explain the trigger, resulting behavior and any limits.
3. Run checks appropriate to the change. Code affecting preparation, rumble,
   controls or packaging needs relevant regression coverage.
4. For runtime or performance changes, record which tests used fixtures and which
   ran on real hardware. Do not describe a component build as gameplay validation.
5. Keep the README, changelog, metadata and release notes consistent with the
   resulting implementation.

AI-assisted contributions are welcome when the contributor understands and can
review the code, discloses assistance where relevant, and provides appropriate
validation. Preserve upstream attribution and license notices.

## Documentation and releases

`README.md` and `docs/` are the canonical documentation. Packaging copies them
into the install archive; do not maintain duplicate copies in the template.
`docs/RELEASE-NOTES.md` describes the current release, while `CHANGELOG.md` records
all published versions.

The manual **Port checks** workflow builds packages and publishes the release
specified in its release step. Before publishing a new version, update that tag,
title, release notes and download links together. Published historical release
assets should remain associated with their original versions.

Document compatibility limits and relevant checks for new features. See
[HARDWARE-VALIDATION.md](docs/HARDWARE-VALIDATION.md).
