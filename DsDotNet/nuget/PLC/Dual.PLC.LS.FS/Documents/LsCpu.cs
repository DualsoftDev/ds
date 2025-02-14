using System;
using XGCommLib;

namespace Dsu.PLC.LS
{
    internal class LsCpu :ICpu
    {
        public CommObject COMObject { get; set; }

        public string Model { get; }
        public bool IsRunning { get {throw new NotImplementedException();} }

        public void Stop() { throw new NotImplementedException(); }
        public void Run() { throw new NotImplementedException(); }

        public LsCpu(CommObject comObject)
        {
            COMObject = comObject;
        }
    }
}
