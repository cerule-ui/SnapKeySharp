using System;
using System.Runtime.InteropServices;
using static SnapKeySharp.Native.NativeMethods;
using SnapKeySharp.Core;

namespace SnapKeySharp.Core
{
    internal class InputSender
    {
        public void SendKey(uint vkCode, bool keyUp)
        {
            INPUT[] inputs =
            {
                new INPUT()
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion()
                    {
                        ki = new KEYBDINPUT()
                        {
                            wVk = (ushort)vkCode,
                            wScan = (ushort)MapVirtualKey(vkCode, 0),
                            dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                            dwExtraInfo = KeyboardHook.MAGIC_EXTRA_INFO
                        }
                    }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}
