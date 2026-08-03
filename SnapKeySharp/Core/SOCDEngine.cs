using System;
using System.Collections.Generic;
using System.Text;
using static SnapKeySharp.Native.NativeMethods;
namespace SnapKeySharp.Core
{
    internal class SOCDEngine
    {
        // поля
        Dictionary<uint, uint> pairs;           // пары (A, D; D, A; ...)

        Dictionary<uint, bool> _physicalState;  // что зажато физически
        Dictionary<uint, bool> _effectiveState; // что зажато системно

        HashSet<string> _excludedProcesses;     // процессы, на которые snapkey не влияет

        private InputSender _inputSender;
        public SOCDEngine(InputSender inputSender)
        {
            _inputSender = inputSender;

            pairs = new Dictionary<uint, uint>();

            _physicalState = new Dictionary<uint, bool>();
            _effectiveState = new Dictionary<uint, bool>();

            _excludedProcesses = new HashSet<string>();
        }

        public bool Process(uint vkCode, bool isKeyDown) // возвращает блокировать или нет
        {
            if (!pairs.ContainsKey(vkCode)) return false; // не наша клавиша - не блокируем

            var currentHandle = GetForegroundWindow(); // хэндл текущего окна
            if (currentHandle != IntPtr.Zero) // если что то запущено
            {
                GetWindowThreadProcessId(currentHandle, out uint pid);
                string processName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
                if (_excludedProcesses.Contains(processName))
                {
                    return false; // процесс находится в списке исключений
                }
            }


            uint partner = pairs[vkCode];

            _physicalState[vkCode] = isKeyDown;

            System.Diagnostics.Debug.WriteLine(
                $"vk={vkCode:X} isDown={isKeyDown} | " +
                $"phys[vk]={_physicalState.GetValueOrDefault(vkCode)} " +
                $"phys[partner]={_physicalState.GetValueOrDefault(partner)} | " +
                $"eff[vk]={_effectiveState.GetValueOrDefault(vkCode)} " +
                $"eff[partner]={_effectiveState.GetValueOrDefault(partner)}"
            );

            if (isKeyDown)
            {
                if (_physicalState.GetValueOrDefault(partner)) // зажали обе клавиши
                {
                    _inputSender.SendKey(partner, keyUp: true); // отжимаем
                    _effectiveState[partner] = false;
                }
                _effectiveState[vkCode] = true;
                return false;
            }
            else
            {
                if (_effectiveState.GetValueOrDefault(vkCode))
                {
                    _effectiveState[vkCode] = false;

                    // партнёр физически зажат но системно отжат? воскрешаем!
                    if (_physicalState.GetValueOrDefault(partner) && !_effectiveState.GetValueOrDefault(partner))
                    {
                        _inputSender.SendKey(partner, keyUp: false);
                        _effectiveState[partner] = true;
                    }

                    return false;
                }
                return true;
            }

        }

        public void AddPair(uint key1, uint key2) // добавляет в словарь pairs новую пару, с проверкой, есть ли там уже она или нет
        {
            if (pairs.ContainsKey(key1) || pairs.ContainsKey(key2))
            {
                return;
            }

            pairs[key1] = key2;
            pairs[key2] = key1;
        }

        public void AddExcludedProcess(string processPath) // добавляет процесс в список исключений
                                                           // можно указать как путь, так и просто название файла (с расширением или без)
        {
            string processName = System.IO.Path.GetFileNameWithoutExtension(processPath);
            _excludedProcesses.Add(processName);
        }

        public void RemovePair(uint key1, uint key2) // убирает пару из словаря pairs
        {
            pairs.Remove(key1);
            pairs.Remove(key2);
        }

        public void RemoveExcludedProcess(string processPath) // убирает процесс из списка исключений
                                                              // можно указать как путь, так и просто название файла (с расширением или без)
        {
            string processName = System.IO.Path.GetFileNameWithoutExtension(processPath);
            _excludedProcesses.Remove(processName);
        }


    }
}
