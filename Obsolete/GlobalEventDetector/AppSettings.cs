using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalEventDetector
{
    internal class AppSettings
    {
        public int EjectIntervalMs { get; set; }
        public string[] EventTypes { get; set; } // one of { "MM", "MD", "MU", "KD", "KU", "KP" };
    }
}
