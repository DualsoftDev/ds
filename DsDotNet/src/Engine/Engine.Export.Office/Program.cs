using Engine.Core;
using Engine.Export.Office;
using Engine.Info;
using Engine.Runtime;
using Engine.TestSimulator;
using System;
using System.Drawing;
using System.IO;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;

namespace Engine.Export.Office
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            string testFile = @$"F:\/HelloDS_new.pptx";
            string templateFile = @$"F:\/DSExport_Template.pptx";
            GenerationPPT.ExportPPT(new DsSystem("testSYS"), templateFile);
            //Console.ReadKey();  
        }
    }
}
