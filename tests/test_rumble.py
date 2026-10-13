"""Exercise the actual helper against a fake motor, including stopped/crashed clients."""
import os
from pathlib import Path
import signal
import subprocess
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]

class RumbleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.binary = Path(cls.tmp.name) / 'rumble'
        subprocess.run(['gcc', '-std=c11', '-Wall', '-Wextra', '-Werror', '-O2',
                        str(ROOT / 'build/src/native/rg34xxsp_rumble.c'), '-o', str(cls.binary)], check=True)

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def setUp(self):
        self.motor = Path(self.tmp.name) / 'motor'
        self.motor.write_text('0\n')
        self.env = dict(os.environ, SDV_RUMBLE_DEVICE=str(self.motor))
        self.process = subprocess.Popen([self.binary], stdin=subprocess.PIPE,
                                        stdout=subprocess.PIPE, env=self.env)
        self.assertEqual(self.process.stdout.readline(), b'READY\n')

    def tearDown(self):
        if self.process.poll() is None:
            self.process.terminate()
            self.process.wait(timeout=2)
        self.process.stdin.close()
        self.process.stdout.close()

    def send(self, data):
        self.process.stdin.write(data)
        self.process.stdin.flush()

    def state(self, value):
        deadline = time.monotonic() + 2
        while time.monotonic() < deadline:
            if self.motor.read_text() == value + '\n': return
            time.sleep(.005)
        self.fail('Motor did not become ' + value)

    def test_start_stop(self):
        self.send(b'1'); self.state('1')
        self.send(b'0'); self.state('0')

    def test_watchdog_and_restart(self):
        self.send(b'1'); self.state('1')
        self.state('0')
        self.send(b'1'); self.state('1')

    def test_client_eof(self):
        self.send(b'1'); self.state('1')
        self.process.stdin.close()
        self.assertEqual(self.process.wait(timeout=2), 0)
        self.state('0')

    def test_signal(self):
        self.send(b'1'); self.state('1')
        self.process.send_signal(signal.SIGTERM)
        self.assertEqual(self.process.wait(timeout=2), 0)
        self.state('0')

    def test_bad_command(self):
        self.send(b'1'); self.state('1')
        self.send(b'x')
        self.assertEqual(self.process.wait(timeout=2), 1)
        self.state('0')

    def test_missing_device(self):
        result = subprocess.run([self.binary, '--probe'], env=dict(self.env, SDV_RUMBLE_DEVICE='/no/such/motor'), capture_output=True)
        self.assertEqual(result.returncode, 1)

if __name__ == '__main__': unittest.main()
