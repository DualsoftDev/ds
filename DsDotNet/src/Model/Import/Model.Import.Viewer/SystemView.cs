using System.Collections.Generic;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public class SystemView
    {
        public string Display { get; set; }
        public DsSystem System { get; set; }
        public List<ViewNode> ViewNodes = new List<ViewNode>();
    }
}