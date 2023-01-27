using Engine.CodeGenCPU;
using Microsoft.Msagl.GraphmapsWithMesh;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class SegmentView
    {
        public string Display { get; set; }
        public Engine.Core.CoreModule.Vertex Vertex { get; set; }
        public ViewNode ViewNode { get; set; }
        public VertexManager VertexM { get; set; }
    }
}