using DevExpress.LookAndFeel;

namespace DSModeler.Utils
{
    [SupportedOSPlatform("windows")]
    public static class EditorSkin
    {
        public static void SetSkin(string name)
        {
            DSRegistry.SetValue(RegKey.RegSkin, name);
        }

        public static string GetSkin()
        {
            return DSRegistry.GetValue(RegKey.RegSkin).ToString();
        }

        internal static void InitSetting(string skinName, string skinPalette)
        {
            UserLookAndFeel.Default.StyleChanged += (s, e) =>
            {
                UserLookAndFeel skin = s as UserLookAndFeel;
                SetSkin($"{skin.ActiveSkinName};{skin.ActiveSvgPaletteName}");
            };

            if (DSRegistry.GetValue(RegKey.RegSkin) == null)
            {
                UserLookAndFeel.Default.SetSkinStyle(skinName, skinPalette);
            }
            else
            {
                string skin = GetSkin();
                string sn = skin.Split(';')[0];
                string sp = skin.Split(';')[1];
                UserLookAndFeel.Default.SetSkinStyle(sn, sp);
            }
        }
    }
}
