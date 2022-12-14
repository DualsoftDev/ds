using Engine.CodeGenCPU;
using static Engine.CodeGenCPU.DsMemoryModule;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class SegmentHMI
    {
        public string Display { get; set; }
        public Vertex Vertex { get; set; }
        public ViewNode ViewNode { get; set; }
        public DsMemory Memory { get; set; }
    }
}