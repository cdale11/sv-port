import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / 'portmaster_stardewvalley_mainline/stardewvalleymainline/tools/cpu-performance'

class CpuPolicyTests(unittest.TestCase):
    def test_performance_governor_restores_original(self):
        with tempfile.TemporaryDirectory() as tmp:
            cpu = Path(tmp); policy = cpu / 'cpufreq/policy0'; policy.mkdir(parents=True)
            (policy / 'scaling_governor').write_text('schedutil\n')
            (policy / 'scaling_available_governors').write_text('performance schedutil powersave\n')
            result = subprocess.run(['bash', '-c', '''
source "$1"
SDV_CPU_AFFINITY=inherit
sdv_cpu_begin
cat "$SDV_CPU_SYSFS_ROOT/cpufreq/policy0/scaling_governor"
sdv_cpu_end
''', 'test', str(HELPER)], env=dict(os.environ, SDV_CPU_SYSFS_ROOT=tmp), capture_output=True, text=True, check=True)
            self.assertIn('performance', result.stdout)
            self.assertEqual((policy / 'scaling_governor').read_text(), 'schedutil\n')

    def test_later_firmware_governor_change_is_preserved(self):
        with tempfile.TemporaryDirectory() as tmp:
            policy = Path(tmp) / 'cpufreq/policy0'; policy.mkdir(parents=True)
            (policy / 'scaling_governor').write_text('schedutil\n')
            (policy / 'scaling_available_governors').write_text('performance schedutil powersave\n')
            subprocess.run(['bash', '-c', '''
source "$1"
SDV_CPU_AFFINITY=inherit
sdv_cpu_begin
printf 'powersave\n' > "$SDV_CPU_SYSFS_ROOT/cpufreq/policy0/scaling_governor"
sdv_cpu_end
''', 'test', str(HELPER)], env=dict(os.environ, SDV_CPU_SYSFS_ROOT=tmp), capture_output=True, check=True)
            self.assertEqual((policy / 'scaling_governor').read_text(), 'powersave\n')

    def test_all_core_affinity_and_missing_taskset_fallback(self):
        with tempfile.TemporaryDirectory() as tmp:
            cpu = Path(tmp); (cpu / 'online').write_text('0-3\n')
            binary = cpu / 'taskset'
            binary.write_text('#!/bin/sh\n[ "$3" = true ] && exit 0\nexit 1\n'); binary.chmod(0o755)
            result = subprocess.run(['bash', '-c', '''
source "$1"
SDV_CPU_GOVERNOR=inherit
sdv_cpu_begin
printf 'command=%s\n' "${sdv_cpu_args[*]}"
''', 'test', str(HELPER)], env=dict(os.environ, SDV_CPU_SYSFS_ROOT=tmp, PATH=tmp+':'+os.environ['PATH']), capture_output=True, text=True, check=True)
            self.assertIn('command=taskset -c 0-3', result.stdout)
            binary.write_text('#!/bin/sh\nexit 1\n')
            result = subprocess.run(['bash', '-c', 'source "$1"; sdv_cpu_begin; printf "command=%s\\n" "${sdv_cpu_args[*]}"',
                                     'test', str(HELPER)], env=dict(os.environ, SDV_CPU_SYSFS_ROOT=tmp, PATH=tmp+':'+os.environ['PATH']), capture_output=True, text=True, check=True)
            self.assertIn('command=\n', result.stdout)

    def test_hud_auto_restores_battery_context_despite_legacy_flag(self):
        profile = ROOT / 'portmaster_stardewvalley_mainline/stardewvalleymainline/tools/rg34xxsp-profile'
        result = subprocess.run(['bash', '-c', '''
SDV_RUMBLE=0
SDV_PERFORMANCE_OVERLAY=0
LD_PRELOAD=/usr/lib/libMangoHud.so:/usr/lib/libinterpose.so
MANGOHUD_CONFIGFILE=/var/run/hud.config
source "$1"
printf 'hud=%s config=%s preload=%s nonhud=%s\n' "$MANGOHUD" "$MANGOHUD_CONFIGFILE" "$LD_PRELOAD" "$SDV_NONHUD_PRELOAD"
''', 'test', str(profile)], capture_output=True, text=True, check=True)
        self.assertIn('hud=1 config=/var/run/hud.config preload=/usr/lib/libMangoHud.so:/usr/lib/libinterpose.so', result.stdout)
        self.assertIn('nonhud=/usr/lib/libinterpose.so', result.stdout)

if __name__ == '__main__': unittest.main()
