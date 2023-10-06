using System;
using System.Collections.Generic;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Import.Office.ViewModule;

namespace Diagram.View.MSAGL
{
    public class ViewVertex
    {
        public Vertex Vertex { get; set; }
        public List<Tuple<ViewNode, UcView>> ViewNodes { get; set; }  //alias 포함
        public Status4 Status { get; set; }
        public List<DsTask> DsTasks { get; set; }
        public bool ErrorTX { get; set; }
        public bool ErrorRX { get; set; }
    }
}


