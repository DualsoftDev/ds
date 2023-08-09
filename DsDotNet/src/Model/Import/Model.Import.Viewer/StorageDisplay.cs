using Engine.CodeGenCPU;
using Microsoft.Msagl.GraphmapsWithMesh;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class StorageDisplay
    {
        public string DisplayUI { get { return $"{Display}{((bool)Value ? "●" : "X")}"; } }
        public string Display { get; set; }
        public IStorage Storage { get; set; }
        public object Value { get; set; }
        public bool OnOff { get; set; }
    }
}