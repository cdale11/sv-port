"""Run the launcher with mocked PortMaster/runtime; no device or retail files."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]

class LauncherTests(unittest.TestCase):
    def run_launcher(self, mode='auto', game_exit=0, patch_exit=0):
        with tempfile.TemporaryDirectory(prefix='sdv launcher ') as tmp:
            root = Path(tmp)
            game = root / 'game'
            shutil.copytree(ROOT / 'portmaster_stardewvalley_mainline/stardewvalleymainline', game)
            pm = root / 'data/PortMaster'; pm.mkdir(parents=True)
            (pm / 'control.txt').write_text('''CFW_NAME=knulli
DEVICE_ARCH=aarch64
CUR_TTY=/dev/null
GPTOKEYB="sleep 20"
sdl_controllerconfig=test-mapping
get_controls() { :; }
bind_directories() { :; }
pm_message() { echo "$*"; }
pm_finish() { echo cleanup >> "$SDV_TEST_RECORD"; }
pm_platform_helper() { echo platform >> "$SDV_TEST_RECORD"; }
''')
            (pm / 'libgl_default.txt').write_text('LIBGL_ES=\n')
            (game / 'rg34xxsp.conf').write_text(f'SDV_LAUNCH_MODE={mode}\nSDV_RUMBLE=0\n')
            data = game / 'gamedata'; data.mkdir(exist_ok=True)
            for name in ['Stardew Valley.dll', 'Stardew Valley.deps.json', 'Stardew Valley.runtimeconfig.json']:
                (data / name).touch()
            dotnet = game / 'dotnet'; dotnet.mkdir(exist_ok=True)
            stub = dotnet / 'dotnet'
            stub.write_text(f'''#!/bin/bash
case "$1" in
  *MainlineGameDataPatcher.dll) echo patch >> "$SDV_TEST_RECORD"; exit {patch_exit} ;;
  *) printf 'game:%s:%s:%s\\n' "$1" "$DISPLAY_WIDTH" "$DISPLAY_HEIGHT" >> "$SDV_TEST_RECORD"; exit {game_exit} ;;
esac
'''); stub.chmod(0o755)
            binaries = root / 'bin'; binaries.mkdir()
            for name, body in [('uname', 'echo aarch64'), ('sleep', 'exit 0')]:
                path = binaries / name; path.write_text('#!/bin/sh\n' + body + '\n'); path.chmod(0o755)
            record = root / 'record'
            env = dict(os.environ, XDG_DATA_HOME=str(root / 'data'), SDV_GAMEDIR=str(game),
                       SDV_TEST_RECORD=str(record), PATH=str(binaries) + ':' + os.environ['PATH'])
            result = subprocess.run(['bash', str(ROOT / 'portmaster_stardewvalley_mainline/StardewValleyMainline.sh')],
                                    env=env, capture_output=True, text=True, timeout=10)
            return result, record.read_text() if record.exists() else ''

    def test_vanilla_and_device_resolution(self):
        result, record = self.run_launcher()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('game:Stardew Valley.dll:720:480', record)
        self.assertIn('platform', record)
        self.assertEqual(record.count('cleanup'), 1)

    def test_game_exit_status(self):
        result, record = self.run_launcher(game_exit=42)
        self.assertEqual(result.returncode, 42)
        self.assertIn('cleanup', record)

    def test_forced_smapi(self):
        result, record = self.run_launcher(mode='smapi')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('game:StardewModdingAPI.dll:720:480', record)

    def test_patcher_failure_never_launches_game(self):
        result, record = self.run_launcher(patch_exit=2)
        self.assertEqual(result.returncode, 1)
        self.assertNotIn('game:', record)
        self.assertIn('cleanup', record)

    def test_bad_mode(self):
        result, record = self.run_launcher(mode='bad')
        self.assertEqual(result.returncode, 1)
        self.assertNotIn('patch', record)

if __name__ == '__main__': unittest.main()
