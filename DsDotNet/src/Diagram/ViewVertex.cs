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
        public List<ViewNode> ViewNodes { get; set; }  //alias 포함
        public ViewNode FlowNode { get; set; }  //UcViewNode
        public Status4 Status { get; set; }
        public List<DsTask> DsTasks { get; set; }
        public bool IsError { get; set; }
        public string ErrorText { get; set; }
    }
}


