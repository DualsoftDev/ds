using DevExpress.XtraSplashScreen;
using Dual.Common.Core;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static Engine.CodeGenCPU.ExportModule;

namespace DSModeler
{
    public static class HMI
    {

     

        public static string Export()
        {
            StringBuilder sb = new StringBuilder();
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
                return "";
            }
           
            SplashScreenManager.ShowForm(typeof(DXWaitForm));

            SplashScreenManager.CloseForm();

            return sb.ToString();
        }
    }
}


