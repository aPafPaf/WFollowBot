using System;

namespace WFollowBot.Settings;

[Serializable]
public class KeyboardLayout
{
    public bool Enabled = false;

    public string KeyLStickUp = "W";
    public string KeyLStickDown = "S";
    public string KeyLStickLeft = "A";
    public string KeyLStickRight = "D";

    public string KeyRStickUp = "NumPad8";
    public string KeyRStickDown = "NumPad2";
    public string KeyRStickLeft = "NumPad4";
    public string KeyRStickRight = "NumPad6";

    public string KeyDPadUp = "ArrowUp";
    public string KeyDPadDown = "ArrowDown";
    public string KeyDPadLeft = "ArrowLeft";
    public string KeyDPadRight = "ArrowRight";
    public string KeyA = "J";
    public string KeyB = "K";
    public string KeyX = "H";
    public string KeyY = "U";
    public string KeyLB = "Q";
    public string KeyRB = "E";
    public string KeyBack = "Tab";
    public string KeyStart = "Enter";
    public string KeyGuide = "F";
    public string KeyL3 = "LShift";
    public string KeyR3 = "Space";
    public string KeyLT = "Z";
    public string KeyRT = "C";
}
