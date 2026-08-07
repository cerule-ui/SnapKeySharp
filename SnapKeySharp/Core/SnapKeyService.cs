using System;
using System.Collections.Generic;
using System.Text;

namespace SnapKeySharp.Core
{
    internal class SnapKeyService
    {
        // сервисы
        InputSender _sender;
        SOCDEngine _engine;
        KeyboardHook _keybdHook;

        public SnapKeyService()
        {
            _sender = new InputSender();
            _engine = new SOCDEngine(_sender);
            _keybdHook = new KeyboardHook();

            _keybdHook.KeyEvent += _engine.Process;
        }

        public void Start()
        {
            _keybdHook.Start();
        }

        public void Stop()
        {
            _keybdHook.Stop();
        }

        public void AddPair(uint key1, uint key2)
        {
            _engine.AddPair(key1, key2);
        }

        public void AddExcludedProcess(string processPath)
        {
            _engine.AddExcludedProcess(processPath);
        }

        public void RemovePair(uint key1, uint key2) => _engine.RemovePair(key1, key2);
        public void RemoveExcludedProcess(string processPath) => _engine.RemoveExcludedProcess(processPath);
        public bool ContainsPair(uint key1, uint key2) => _engine.ContainsPair(key1, key2);
        public bool ExcludedProcessesExist() => _engine.ExcludedProcessesExist();
        public bool PairsExist() => _engine.PairsExist();
    }
}
