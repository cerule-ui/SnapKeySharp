using System;
using System.Collections.Generic;
using System.Text;

namespace SnapKeySharp.Services
{
    public class AppConfig
    {
        public bool IsActive { get; set; } = true;
        public bool AutoStart { get; set; } = false;

        public List<string> Pairs { get; set; } = new List<string>(); // 41,44 -> A,D

        public List<string> Exclusions { get; set; } = new List<string>();
    }
}
