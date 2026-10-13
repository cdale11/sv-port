// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework
{
    internal static class PortMasterRuntime
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        internal static readonly bool TextureUploadSync = Environment.GetEnvironmentVariable("SDV_TEXTURE_UPLOAD_SYNC") == "1";
        internal static readonly int SpriteBatchCapacity = ReadCapacity();
        private static readonly bool Diagnostics = Environment.GetEnvironmentVariable("SDV_STARTUP_DIAGNOSTICS") == "1";

        private static int ReadCapacity()
        {
            int value;
            if (!int.TryParse(Environment.GetEnvironmentVariable("SDV_SPRITE_BATCH_CAPACITY"), out value)) value = 256;
            value = Math.Max(256, Math.Min(4096, value));
            return (value + 63) & ~63;
        }

        internal static void Stage(string stage)
        {
            if (Diagnostics) Console.WriteLine("Startup stage: " + stage + " at " + DateTimeOffset.Now.ToString("O") +
                "; framework elapsed=" + Clock.ElapsedMilliseconds + "ms");
        }
    }
}
