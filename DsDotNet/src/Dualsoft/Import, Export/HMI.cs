using DevExpress.XtraSplashScreen;
using System.Text;

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
            Global.Logger.Warn("Web Server 시작이 필요합니다.");

            SplashScreenManager.CloseForm();

            return sb.ToString();
        }
    }
}


