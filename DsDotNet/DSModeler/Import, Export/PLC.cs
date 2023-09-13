
namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class PLC
{



    public static string Export()
    {
        if (!Global.IsLoadedPPT())
        {
            Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
            return "";
        }
        string newPath = "";

        try
        {
            SplashScreenManager.ShowForm(typeof(DXWaitForm));

            string xmlTemplateFile = Path.ChangeExtension(Files.GetLast().First(), "xml");
            string xmlFileName = Path.GetFileName(xmlTemplateFile);
            string xmlDriectory = Path.GetDirectoryName(xmlTemplateFile);
            string fullpath = Path.Combine(xmlDriectory, xmlFileName);
            newPath = Files.GetNewFileName(fullpath, "PLC");
            Global.ExportPathPLC = newPath;
            if (File.Exists(xmlTemplateFile))
            {
                //사용자 xg5000 Template 형식으로 생성
                ExportModuleExt.ExportXMLforXGI(Global.ActiveSys, newPath, xmlTemplateFile);
            }
            else  //기본 템플릿 CPU-E 타입으로 생성
            {
                ExportModuleExt.ExportXMLforXGI(Global.ActiveSys, newPath, null);
            }
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


