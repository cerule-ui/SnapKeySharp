using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using static SnapKeySharp.Native.NativeMethods;

namespace SnapKeySharp.Core
{
    internal class KeyboardHook
    {
        private IntPtr _hookHandle;                                      // наш хэндл
        private LowLevelKeyboardProc _callback;                          // храним делегат в отдельной переменной, что бы избежать очистки в памяти
        public static readonly IntPtr MAGIC_EXTRA_INFO = (IntPtr)0xCAFE; // наш маркер синтетических событий

        public event Func<uint, bool, bool>? KeyEvent; // vkCode, isKeyDown -> shouldBlock

        public void Start() 
        {
            _callback = HookCallback;
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(null), 0);
        }

        public void Stop() 
        {
            UnhookWindowsHookEx(_hookHandle);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) // функция вызывается при нажатии любой клавишы
        {
            if (nCode < 0) // если действие срочное
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);  // немедленно отпускаем его
            }

            var kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam); // распаковывем структуру

            if (kbStruct.dwExtraInfo.Equals(MAGIC_EXTRA_INFO)) // если это наш ввод
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);  // не обрабатывая посылаем дальше в систему
            }

            bool isKeyDown = (uint)wParam == WM_KEYDOWN || (uint)wParam == WM_SYSKEYDOWN; 

            bool block = KeyEvent?.Invoke(kbStruct.vkCode, isKeyDown) ?? false; // ?? - значит если слева null то сделать false

            return block ? (IntPtr)1 : CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }
    }
}
