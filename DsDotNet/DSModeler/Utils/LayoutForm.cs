using DevExpress.XtraBars.Docking;

namespace DSModeler.Utils;
[SupportedOSPlatform("windows")]
public static class LayoutForm
{

    internal static void LoadLayout(DockManager docM)
    {
        if (!Directory.Exists(Global.DefaultAppSettingFolder))
        {
            _ = Directory.CreateDirectory(Global.DefaultAppSettingFolder);
        }

        docM.BeginUpdate();
        docM.SaveLayoutToXml($"{Global.DefaultAppSettingFolder}\\default_layout.xml");
        if (File.Exists($"{Global.DefaultAppSettingFolder}\\layout.xml"))
        {
            docM.RestoreLayoutFromXml($"{Global.DefaultAppSettingFolder}\\layout.xml");
        }

        docM.EndUpdate();
    }

    internal static void SaveLayout(DockManager docM)
    {
        docM.SaveLayoutToXml($"{Global.DefaultAppSettingFolder}\\layout.xml");
    }
    internal static void RestoreLayoutFromXml(DockManager docM)
    {
        docM.RestoreLayoutFromXml($"{Global.DefaultAppSettingFolder}\\default_layout.xml");
        docM.ForceInitialize();
    }

}


