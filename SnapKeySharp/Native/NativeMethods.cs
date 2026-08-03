using System;
using System.Runtime.InteropServices;
namespace SnapKeySharp.Native
{
    internal static class NativeMethods
    {
        // --- константы ---
        public const int WH_KEYBOARD_LL = 13;      // идентификатор низкого уровня клавиатуры

        public const uint WM_KEYDOWN = 0x0100;    // сообщение о нажатии клавишы
        public const uint WM_KEYUP = 0x0101;      // сообщение о отпускании клавишы
        public const uint WM_SYSKEYDOWN = 0x0104; // сообщение о нажатии системной клавиши (alt, ctrl, ...)
        public const uint WM_SYSKEYUP = 0x0105;   // сообщение о отпускании системной клавиши

        public const uint KEYEVENTF_KEYUP = 0x0002; // флаг для симуляции отпускания клавиши

        public const uint INPUT_MOUSE    = 0; // виртуальный ввод для мыши
        public const uint INPUT_KEYBOARD = 1; // виртуальный ввод для клавиатуры
        public const uint INPUT_HARDWARE = 2; // виртуальный ввод для аппаратных устройств


        // --- структуры ---
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT 
            // перехват ввода
        {
            public uint vkCode;   // виртуальный код клавиши
            public uint scanCode; // скан-код клавиши (зависит от самой клавиатуры)

            public uint flags; // набор флагов, описывающих состояние клавиши:
                               // Бит 0 (LLKHF_EXTENDED): Указывает, является ли клавиша расширенной (например, правый Ctrl или Alt).
                               // Бит 4 (LLKHF_INJECTED): Показывает, было ли событие искусственно симулировано (например, через SendInput).
                               // Бит 5 (LLKHF_ALTDOWN): Нажата ли системная клавиша ALT.
                               // Бит 7 (LLKHF_UP): Отпущена ли клавиша.

            public uint time;         // временная метка (в миллисекундах от старта системы)
            public IntPtr dwExtraInfo; // доп информация (может быть поставлена при сммуляции ввода)
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT  
            // контейнер, который система использует в SendInput для симуляции ввода
        {
            public uint type; // тип события, принимает три значения:
                              // INPUT_MOUSE (0):    Будет использоваться поле mi (мышь)
                              // INPUT_KEYBOARD (1): Будет использоваться поле ki (клавиатура)
                              // INPUT_HARDWARE (2): Будет использоваться поле hi (аппаратное сообщение)

            public InputUnion U; // юнион, у которого будет такой тип, который был указан в type
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            // используем [FieldOffset(0)], что бы система понимала, что это все с самого начала записывается, и здесь будет только один тип данных
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
            // эта структура находится внутри INPUT, если type == 0
            // используется для симуляции движений мыши, кликов и вращения колеса
        {
            public int dx; // координата x (абсолютная или относительная)
            public int dy; // координата y (абсолютная или относительная)

            public uint mouseData; // данные колесика (wheel delta) или кнопок XBUTTON
            public uint dwFlags;   // флаги действий (MOUSEEVENTF_MOVE, MOUSEEVENTF_LEFTDOWN и т.д.)
            public uint time;      // временная метка (0 - авто, подробнее в KEYBDINPUT)

            public IntPtr dwExtraInfo; // доп информация (может быть поставлена при сммуляции ввода)

        }


        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
            // эта структура находится внутри INPUT, если type == 1
            // описывает конкретное действие, которое нужно программно сгенерировать
        {
            public ushort wVk;     // виртуальный код клавиши
            public ushort wScan;   // скан код клавиши (зависит от самой клавиатуры)

            public uint dwFlags; // флаги, управляющие процессом нажатия
                                 // 0 (по умолчанию)            - нажатие клавиши
                                 // KEYEVENTF_KEYUP (0x0002)    - отпускание клавиши
                                 // KEYEVENTF_SCANCODE (0x0008) - говорит системе, что нам надо использовать поле wScan, вместо wVk
                                 // KEYEVENTF_UNICODE (0x0004)  - используется для ввода текста (символ юникода записывается в wScan)

            public uint time;         // временная метка события. обычно передают 0, тогда система сама поставит текущее время
            public IntPtr dwExtraInfo; // доп информация (может быть поставлена при сммуляции ввода)
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        // эта структура находится внутри INPUT, если type == 2
        // используется КРАЙНЕ РЕДКО.

        // она предназначена для симуляции ввода от нестандартных аппаратных устройств
        // (например, специализированных пультов или старых световых перьев),
        // которые передают сообщения напрямую на входной поток Windows.
        {
            public uint uMsg;      // сообщение
            public ushort wParamL; // младший брат сообщения
            public ushort wParamH; // старший брат сообщения
        }



        // --- делегаты ---
        public delegate IntPtr LowLevelKeyboardProc( // каждый раз, когда пользователь нажимает или отпускает клавишу, windows вызывает метод, привязанный к этому делегату
            int nCode,    // отражает состояние обработки.
                          // Если nCode < 0 - мы ОБЯЗАНЫ проигнорировать сообщение и немедленно вернуть результат функции CallNextHookEx
                          // Если nCode == 0 (HC_ACTION) - значит, что параметры wParam и lParam
                          // содержат актуальную информацию о нажатой клавише

            IntPtr wParam, // идентификатор сообщения клавиатуры (WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN или WM_SYSKEYUP)
            IntPtr lParam  // указатель на структуру в памяти.
                           // при низкоуровневом хуке клавиатуры (WH_KEYBOARD_LL) этот указатель всегда ссылается на
                           // структуру KBDLLHOOKSTRUCT

            ); // обычно возвращается результат выполнения CallNextHookEx, чтобы передать нажатие другим программам в системе
               // если вернуть ненулевое значение (например (IntPtr)1), мы заблокируем нажатие.
               // и ни одна программа (даже windows) не узнает, что клавиша была нажата.
               // так делают, для создания системных запретов



        // --- методы ---
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle( // возвращает дескриптор (handle) уже загруженного в память модуля (.exe или .dll) по имени
            string lpModuleName // имя модуля, если передать null - вернет handle текущего процесса (.exe)
            ); // !!! если вернул IntPtr.Zero - модуль не найден


        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx( // регистрирует нашу функцию обратного вызова в цепочке хуков windows
            int idHook,                // тип хука (для низкоуровневой клавиатуры передают WH_KEYBOARD_LL (13))
            LowLevelKeyboardProc lpfn, // ссылка на наш делегат, который будет обрабатывать события

            IntPtr hMod,               // дескриптор библиотеки, содержащей колбэк. 
                                       // для низкоуровневых хуков сюда передают дескриптор текущего запущенного модуля (GetModuleHandle)

            uint dwThreadId            // идентификатор потока, который нужно отслеживать
                                       // если поставить 0, хук станет глобальным и будет перехватывать ввод во всей системе
            ); // возвращает IntPtr (хэндл хука)
               // его надо сохранить, что бы потом передать в CallNextHookEx,
               // а затем удалить через UnhookWindowsHookEx.
               // !!! если вернулся IntPtr.Zero - то установка сорвалась


        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx( // передает информацию о нажатии следующему перехватчику в цепочке или самой ОС
            IntPtr hhk,    // хэндл текущего хука
            int nCode,     // то, что пришло в делегат
            IntPtr wParam, // то, что пришло в делегат
            IntPtr lParam  // то, что пришло в делегат
            ); // !!! если не вызвать этот метод, то нажатие заблокируется.
               // поэтому, если нет цели намеренно блокировать ввод, этот метод обязательно вызывается сразу после обработки нажатия


        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx( // удаляет наш хук из системной цепочки
            IntPtr hhk // дескриптор хука, полученный ранее из SetWindowsHookEx
            ); // !!! обязательно вызывается при закрытии программы.
               // если хук не снять, виндовс со временем сама выгрузит его, заметив зависание,
               // но конкретное освобождение ресурсов предотвращает утечки памяти и лаги клавиатуры у пользователя


        [DllImport("user32.dll")]
        public static extern uint SendInput( // основной метод для программного нажатия клавиш и перемещения мыши
            uint nInputs,    // количество структур в массиве (сколько действий отправляем за раз)
            INPUT[] pInputs, // массив структур INPUT (которые содержат KEYBDINPUT или MOUSEINPUT)
            int cbSize       // размер одной структуры в байтах (Marshal.SizeOf(typeof(INPUT))
            ); // возвращает кол-во успешно вставленных событий.
               // !!! если вернулся 0 - ввод был заблокирован (например, если нет прав администратора, или политик безопасности UIPI)


        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState( // определяет, нажата ли клавиша в данный момент времени,
                                                     // и нажималась ли она после предыдущего вызова этой функции
            int vKey // виртуальный код клавиши
            ); // возвращает значение типа short (16 бит)
               // !!! проверять нужно отдельные биты:
               // старший бит (результат < 0) - клавиша зажата прямо сейчас, проверка идет вот так: (GetAsyncKeyState(vKey) & 0x8000) != 0
               // младший бит (биты равен 1)  - клавиша была зажата после предыдущего вызова GetAsyncKeyState
               // !!! (младший этот бит часто работает нестабильно в современных ОС, поэтому полагаться лучше на старший бит.


        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey( // переводит виртуальный код клавиши в scan code и обратно, в зависимости от режима
            uint uCode,    // входное значение (vkCode или scanCode — зависит от uMapType)
            uint uMapType  // режим перевода:
                           // 0 (MAPVK_VK_TO_VSC)    - виртуальный код -> scan code
                           // 1 (MAPVK_VSC_TO_VK)    - scan code -> виртуальный код
                           // 2 (MAPVK_VK_TO_CHAR)   - виртуальный код -> символ (например, 0x41 → 'A')
                           // 3 (MAPVK_VSC_TO_VK_EX) - scan code -> виртуальный код с учётом расширенных клавиш
            ); // возвращает результат перевода,
               // !!! может вернуть 0 если перевод невозможен


        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow(); // возвращает хэндл окна, которое сейчас в фокусе (активное окно)
                                                           // !!! если нет активного окна - вернёт IntPtr.Zero


        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId( // возвращает идентификатор потока, создавшего окно, и записывает PID процесса
            IntPtr hWnd,           // хэндл окна (например, полученный из GetForegroundWindow)
            out uint lpdwProcessId // сюда запишется PID процесса-владельца окна
            ); // возвращает идентификатор потока
               // !!! нас интересует именно lpdwProcessId, а не возвращаемое значение
    }
}
