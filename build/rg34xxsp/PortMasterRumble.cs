// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Xna.Framework.Input
{
    // A separate helper owns the motor so a crashed or stopped game cannot
    // leave it running. External pads retain MonoGame's SDL haptic handling.
    internal static class PortMasterRumble
    {
        private static readonly object Sync = new object();
        private static Process helper;
        private static bool attempted;
        private static bool active;
        private static long lastCommand;
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        internal static bool Available(int index)
        {
            int selected;
            if (!int.TryParse(Environment.GetEnvironmentVariable("SDV_RUMBLE_PLAYER"), out selected)) selected = 0;
            if (index != selected) return false;
            lock (Sync)
            {
                if (!attempted)
                {
                    attempted = true;
                    string path = Environment.GetEnvironmentVariable("SDV_RUMBLE_HELPER");
                    if (string.IsNullOrEmpty(path)) return false;
                    try
                    {
                        helper = Process.Start(new ProcessStartInfo(path) {
                            UseShellExecute = false, RedirectStandardInput = true,
                            RedirectStandardOutput = true, CreateNoWindow = true
                        });
                        if (helper.StandardOutput.ReadLine() != "READY") { Close(); return false; }
                        AppDomain.CurrentDomain.ProcessExit += (sender, args) => Close();
                        Console.WriteLine("RG34XXSP KNULLI rumble ready (player " + selected + ").");
                    }
                    catch (Exception ex) { Console.WriteLine("Rumble disabled: " + ex.Message); Close(); }
                }
                return helper != null;
            }
        }

        internal static bool Set(int index, float left, float right)
        {
            if (!Available(index)) return false;
            lock (Sync)
            {
                bool on = left > 0 || right > 0;
                long now = Clock.ElapsedMilliseconds;
                if (on == active && (!on || now - lastCommand < 50)) return true;
                try
                {
                    helper.StandardInput.Write(on ? '1' : '0');
                    helper.StandardInput.Flush();
                    lastCommand = now;
                    active = on;
                    return true;
                }
                catch (IOException) { Close(); return false; }
                catch (InvalidOperationException) { Close(); return false; }
            }
        }

        internal static void Close()
        {
            lock (Sync)
            {
                if (helper == null) return;
                try { helper.StandardInput.Close(); } catch (IOException) { }
                helper.Dispose();
                helper = null;
                active = false;
            }
        }
    }
}
