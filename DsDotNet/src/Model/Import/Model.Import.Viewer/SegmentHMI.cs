using Engine.CodeGenCPU;
using Microsoft.Msagl.GraphmapsWithMesh;
using static Engine.CodeGenCPU.VertexManagerModule;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class SegmentHMI
    {
        public string Display { get; set; }
        public Engine.Core.CoreModule.Vertex Vertex { get; set; }
        public ViewNode ViewNode { get; set; }
        public VertexManager VertexM { get; set; }
    }
}