using DocumentFormat.OpenXml.Packaging;
using Engine.CodeGenCPU;
using LanguageExt;
using Microsoft.Msagl.GraphmapsWithMesh;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class StorageDisplay
    {
        public string DisplayUI
        {
            get
            {
                var t = TagKindExt.GetVertexTagKind(Storage);
                if (t != null)
                    return $"{((bool)Value ? "●" : "X")}    {t.Value}\t{Display}";
                else
                    return $"{((bool)Value ? "●" : "X")} \t\t{Display}";
            }
        }
        public string Display { get; set; }
        public IStorage Storage { get; set; }
        public object Value { get; set; }
        public bool OnOff { get; set; }
    }
}