using DevExpress.LookAndFeel;
using DevExpress.Skins;

namespace DSModeler
{

    public static class EditorSkin
    {
        public static void SetSkin(string name)
        {
            DSRegistry.SetValue(K.RegSkin, name);
        }

        public static string GetSkin()
        {
            return DSRegistry.GetValue(K.RegSkin).ToString();
        }

        internal static void InitSetting(string skinName, string skinPalette)
        {
            UserLookAndFeel.Default.StyleChanged += (s, e) =>
            {
                var skin = s as UserLookAndFeel;
                EditorSkin.SetSkin($"{skin.ActiveSkinName};{skin.ActiveSvgPaletteName}");
            };

            if (DSRegistry.GetValue(K.RegSkin) == null)
                UserLookAndFeel.Default.SetSkinStyle(skinName, skinPalette);
            else
            {
                var skin = GetSkin();
                var sn = skin.Split(';')[0];
                var sp = skin.Split(';')[1];
                UserLookAndFeel.Default.SetSkinStyle(sn, sp);
            }
        }
    }
}
