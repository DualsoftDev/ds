using System.Collections.Generic;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class DiagramView
    {
        public string Display { get; set; }
        public DsSystem System { get; set; }
        public List<ViewNode> ViewNodes = new List<ViewNode>();
    }
    public class SystemView : DiagramView
    {

    }

    public class DeviceView : DiagramView
    {

    }
}