using DevExpress.XtraBars.Docking;
using System.IO;

namespace Dualsoft
{
    public static class LayoutForm
    {

        internal static void LoadLayout(DockManager docM)
        {
            if (!Directory.Exists(GlobalStatic.DefaultAppSettingFolder))
                Directory.CreateDirectory(GlobalStatic.DefaultAppSettingFolder);

            docM.BeginUpdate();
            docM.SaveLayoutToXml($"{GlobalStatic.DefaultAppSettingFolder}\\default_layout.xml");
            if (File.Exists($"{GlobalStatic.DefaultAppSettingFolder}\\layout.xml"))
                docM.RestoreLayoutFromXml($"{GlobalStatic.DefaultAppSettingFolder}\\layout.xml");
            docM.EndUpdate();
        }

        internal static void SaveLayout(DockManager docM)
        {
            docM.SaveLayoutToXml($"{GlobalStatic.DefaultAppSettingFolder}\\layout.xml");
        }
    }
}


