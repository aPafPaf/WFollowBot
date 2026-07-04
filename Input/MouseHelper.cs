using System.Runtime.InteropServices;

namespace WFollowBot.Input;

public class MouseHelper
{
    private const int VK_RBUTTON = 0x02;

    private bool _previousRightState;
    private bool _currentRightState;

    public void Update()
    {
        _previousRightState = _currentRightState;
        _currentRightState = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
    }

    public bool IsRightButtonDown()
    {
        return _currentRightState;
    }

    public bool IsRightClicked()
    {
        return _currentRightState && !_previousRightState;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
