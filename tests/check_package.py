"""Validate a retail-free install ZIP, its checksums and native architecture."""
import hashlib
from pathlib import PurePosixPath
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1]) as package:
    names = set(package.namelist())
    base = 'stardewvalleymainline/'
    for name in ['StardewValleyMainline.sh', 'README.md', 'LICENSE', 'CHANGELOG.md',
                 'docs/CREDITS.md', 'docs/HARDWARE-VALIDATION.md', 'docs/RELEASE-NOTES.md',
                 'BUILD-STATUS.txt', 'SHA256SUMS'] + [base + p for p in [
        'libs.aarch64/rg34xxsp-rumble', 'tools/rg34xxsp-profile',
        'tools/diagnose-rg34xxsp', 'tools/preparation-cache', 'tools/cpu-performance', 'dotnet/dotnet',
        'overrides/gamedata/MonoGame.Framework.dll',
        'tools/MainlineGameDataPatcher/MainlineGameDataPatcher.runtimeconfig.json',
        'tools/SMAPIBundle/game-root/StardewModdingAPI.dll']]:
        assert name in names, f'Missing {name}'
    for name in names:
        assert not name.startswith('/') and '..' not in PurePosixPath(name).parts, name
        assert not name.startswith(base + 'gamedata/') or name.endswith(('/', '.gitkeep')), f'Retail payload: {name}'
        data = package.read(name)
        if data.startswith(b'\x7fELF'):
            assert data[4:6] == b'\x02\x01' and int.from_bytes(data[18:20], 'little') == 183, f'Non-ARM64 ELF: {name}'
    for line in package.read('SHA256SUMS').decode().splitlines():
        digest, name = line.split('  ', 1)
        assert hashlib.sha256(package.read(name.removeprefix('./'))).hexdigest() == digest, name
    framework = package.read(base + 'overrides/gamedata/MonoGame.Framework.dll')
    assert b'PortMasterRumble' in framework, 'Missing MonoGame rumble integration'
    assert b'PortMasterRuntime' in framework, 'Missing framework performance controls'
    for name in ['StardewValleyMainline.sh', base + 'dotnet/dotnet', base + 'libs.aarch64/rg34xxsp-rumble']:
        assert package.getinfo(name).external_attr >> 16 & 0o111, f'Not executable: {name}'
    print(f'PASS: {len(names)} archive entries; payload checksums, ARM64 ELF, rumble patch, executable modes, no retail files')
