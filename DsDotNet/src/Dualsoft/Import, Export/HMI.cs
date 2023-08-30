using DevExpress.XtraSplashScreen;
using System.Text;
using Engine.CodeGenHMI;
using Newtonsoft.Json;

namespace DSModeler
{
    public static class HMI
    {
        public static string Export(FormMain formMain)
        {
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
                return "";
            }
            
            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            var model = formMain.Model;
            var hmiGenModule = new HmiGenModule.HmiCode(model);
            var json = CodeGenHandler.JsonWrapping(hmiGenModule.Generate());
            SplashScreenManager.CloseForm();
            return json.ToString();
        }
    }
}