"""Run the launcher with mocked PortMaster/runtime; no device or retail files."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]

class LauncherTests(unittest.TestCase):
    def run_launcher(self, mode='auto', game_exit=0, patch_exit=0, runs=1, before_run=None):
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
            (data / 'Content').mkdir()
            for name in ['Stardew Valley.dll', 'Stardew Valley.deps.json', 'Stardew Valley.runtimeconfig.json']:
                (data / name).touch()
            patcher = game / 'tools/MainlineGameDataPatcher'
            patcher.mkdir(exist_ok=True)
            (patcher / 'MainlineGameDataPatcher.dll').write_text('patcher')
            dotnet = game / 'dotnet'; dotnet.mkdir(exist_ok=True)
            stub = dotnet / 'dotnet'
            stub.write_text(f'''#!/bin/bash
case "$1" in
  *MainlineGameDataPatcher.dll) echo patch >> "$SDV_TEST_RECORD"; exit {patch_exit} ;;
  *) printf 'game:%s:%s:%s\\n' "$1" "$DISPLAY_WIDTH" "$DISPLAY_HEIGHT" >> "$SDV_TEST_RECORD"; exit {game_exit} ;;
esac
'''); stub.chmod(0o755)
            binaries = root / 'bin'; binaries.mkdir()
            cpus = root / 'cpu'; cpus.mkdir()
            (cpus / 'online').write_text('0-3\n')
            for name, body in [('uname', 'echo aarch64'), ('sleep', 'exit 0')]:
                path = binaries / name; path.write_text('#!/bin/sh\n' + body + '\n'); path.chmod(0o755)
            record = root / 'record'
            env = dict(os.environ, XDG_DATA_HOME=str(root / 'data'), SDV_GAMEDIR=str(game),
                       SDV_TEST_RECORD=str(record), SDV_CPU_SYSFS_ROOT=str(cpus), PATH=str(binaries) + ':' + os.environ['PATH'])
            for run in range(runs):
                if before_run: before_run(game, run, env)
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

    def test_unchanged_second_launch_skips_patcher(self):
        result, record = self.run_launcher(runs=2)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 1)
        self.assertIn('Preparation cache hit', result.stdout)

    def test_changed_game_and_patcher_invalidate_cache(self):
        for relative in ['gamedata/Stardew Valley.dll', 'gamedata/libSDL2-2.0.so.0', 'overrides/gamedata/MonoGame.Framework.dll',
                         'tools/MainlineGameDataPatcher/MainlineGameDataPatcher.dll']:
            def change(game, run, env):
                if run == 1: (game / relative).write_text('changed input')
            result, record = self.run_launcher(runs=2, before_run=change)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(record.splitlines().count('patch'), 2, relative)

    def test_changed_mod_assembly_invalidates_smapi_cache(self):
        def change(game, run, env):
            mod = game / 'Mods/NewMod'; mod.mkdir(exist_ok=True)
            (mod / 'Mod.dll').write_text('assembly version ' + str(run))
        result, record = self.run_launcher(mode='smapi', runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 2)

    def test_metadata_failure_falls_back_to_preparation(self):
        def change(game, run, env):
            if run == 1:
                stat = game.parent / 'bin/stat'
                stat.write_text('#!/bin/sh\nexit 1\n'); stat.chmod(0o755)
        result, record = self.run_launcher(runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 2)
        self.assertIn('Preparation cache unavailable', result.stdout)

    def test_new_mod_switches_mode_and_invalidates_cache(self):
        def change(game, run, env):
            if run == 1:
                mod = game / 'Mods/NewMod'; mod.mkdir()
                (mod / 'manifest.json').write_text('{"UniqueId":"Fixture.NewMod"}')
        result, record = self.run_launcher(runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 2)
        self.assertIn('game:StardewModdingAPI.dll', record)

    def test_force_prepare(self):
        def change(game, run, env): env['SDV_FORCE_PREPARE'] = '1'
        result, record = self.run_launcher(runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 2)

    def test_jit_setting_change_invalidates_preparation(self):
        def change(game, run, env): env['SDV_QUICKJIT_LOOPS'] = str(run)
        result, record = self.run_launcher(runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 2)

    def test_governor_restored_after_game_failure(self):
        def change(game, run, env):
            policy = game.parent / 'cpu/cpufreq/policy0'
            if run == 0:
                policy.mkdir(parents=True)
                (policy / 'scaling_available_governors').write_text('performance schedutil\n')
                (policy / 'scaling_governor').write_text('schedutil\n')
            else:
                self.assertEqual((policy / 'scaling_governor').read_text(), 'schedutil\n')
        result, record = self.run_launcher(game_exit=42, runs=2, before_run=change)
        self.assertEqual(result.returncode, 42)
        self.assertEqual(record.splitlines().count('cleanup'), 2)

    def test_failure_is_not_cached(self):
        result, record = self.run_launcher(patch_exit=2, runs=2)
        self.assertEqual(result.returncode, 1)
        self.assertEqual(record.splitlines().count('patch'), 2)

    def test_content_changes_do_not_repatch_assemblies(self):
        def change(game, run, env):
            if run == 1: (game / 'gamedata/Content/texture.xnb').write_text('updated texture')
        result, record = self.run_launcher(runs=2, before_run=change)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(record.splitlines().count('patch'), 1)

    def test_overlay_filter_preserves_other_preloads(self):
        profile = ROOT / 'portmaster_stardewvalley_mainline/stardewvalleymainline/tools/rg34xxsp-profile'
        result = subprocess.run(['bash', '-c', '''
SDV_RUMBLE=0
SDV_HUD=off
LD_PRELOAD=/usr/lib/libMangoHud.so:/usr/lib/libinterpose.so
source "$1"
printf 'preload=%s overlay=%s\n' "$LD_PRELOAD" "$MANGOHUD"
''', 'test', str(profile)], capture_output=True, text=True, check=True)
        self.assertIn('preload=/usr/lib/libinterpose.so overlay=0', result.stdout)

    def test_overlay_opt_in_preserves_mangohud(self):
        profile = ROOT / 'portmaster_stardewvalley_mainline/stardewvalleymainline/tools/rg34xxsp-profile'
        result = subprocess.run(['bash', '-c', '''
SDV_RUMBLE=0
SDV_HUD=on
LD_PRELOAD=/usr/lib/libMangoHud.so:/usr/lib/libinterpose.so
source "$1"
printf 'preload=%s\n' "$LD_PRELOAD"
''', 'test', str(profile)], capture_output=True, text=True, check=True)
        self.assertIn('preload=/usr/lib/libMangoHud.so:/usr/lib/libinterpose.so', result.stdout)

if __name__ == '__main__': unittest.main()
