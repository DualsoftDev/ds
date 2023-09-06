using DevExpress.XtraSplashScreen;
using Dual.Common.Core;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Engine.CodeGenCPU.ExportModule;

namespace DSModeler
{
    public static class PLC
    {

        public static void OpenPLCFolder()
        {
            if (Global.IsLoadedPPT() && !Global.ExportPathPLC.IsNullOrEmpty())
                Process.Start(Path.GetDirectoryName(Global.ExportPathPLC));
        }

        public static string Export()
        {
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
                return "";
            }
            var newPath = "";

            try
            {
                SplashScreenManager.ShowForm(typeof(DXWaitForm));

                var xmlTemplateFile = Path.ChangeExtension(Files.GetLast().First(), "xml");
                var xmlFileName = Path.GetFileName(xmlTemplateFile);
                var xmlDriectory = Path.GetDirectoryName(xmlTemplateFile);
                var fullpath = Path.Combine(xmlDriectory, xmlFileName);
                newPath = Files.GetNewFileName(fullpath, "PLC");
                Global.ExportPathPLC = newPath;
                if (File.Exists(xmlTemplateFile))
                    //사용자 xg5000 Template 형식으로 생성
                    ExportModuleExt.ExportXMLforXGI(Global.ActiveSys, newPath, xmlTemplateFile);
                else  //기본 템플릿 CPU-E 타입으로 생성
                    ExportModuleExt.ExportXMLforXGI(Global.ActiveSys, newPath, null);

            }
            catch (System.Exception)
            {
                throw;
            }

            finally 
            {
                SplashScreenManager.CloseForm();
            }

            return newPath;
        }
    }
}


