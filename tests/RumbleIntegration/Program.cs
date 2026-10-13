using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Input;

string motor = Path.GetTempFileName();
Environment.SetEnvironmentVariable("SDV_RUMBLE_DEVICE", motor);
Environment.SetEnvironmentVariable("SDV_RUMBLE_HELPER", args[0]);
Environment.SetEnvironmentVariable("SDV_RUMBLE_PLAYER", "0");
void Check(bool value, string message) { if (!value) throw new Exception(message); }
void WaitFor(string value)
{
    for (int n = 0; n < 200; n++) { if (File.ReadAllText(motor) == value + "\n") return; Thread.Sleep(5); }
    throw new Exception("Motor never reached " + value);
}
try
{
    Check(!PortMasterRumble.Available(1), "External pad was routed to built-in motor");
    Check(PortMasterRumble.Available(0), "Helper handshake failed");
    Check(PortMasterRumble.Set(0, .5f, 0), "Start failed"); WaitFor("1");
    Check(PortMasterRumble.Set(0, 0, 0), "Stop failed"); WaitFor("0");
    Check(PortMasterRumble.Set(0, 0, 1), "Right motor failed"); WaitFor("1");
    WaitFor("0"); // frozen/stopped client watchdog
    Thread.Sleep(60);
    Check(PortMasterRumble.Set(0, 1, 0), "Resume failed"); WaitFor("1");
    PortMasterRumble.Close(); WaitFor("0");
    Console.WriteLine("PASS: managed vibration -> native helper -> motor, routing, timeout, shutdown");
}
finally { PortMasterRumble.Close(); File.Delete(motor); }
