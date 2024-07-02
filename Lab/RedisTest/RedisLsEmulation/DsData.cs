using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisPLC
{
    [System.Serializable]
    public class DsInterface
    {
        public int Id { get; set; }
        public string Work { get; set; }
        public string WorkInfo { get; set; }
        public string ScriptStartTag { get; set; }
        public string ScriptEndTag { get; set; }
        public string MotionStartTag { get; set; }
        public string MotionEndTag { get; set; }
        public string Station { get; set; }
        public string Device { get; set; }
        public string Action { get; set; }
        public string LibraryPath { get; set; }
        public string Motion { get; set; }
    }

    [System.Serializable]
    public class DsData
    {
        public List<DsInterface> DsInterfaces { get; set; }
    }
}
