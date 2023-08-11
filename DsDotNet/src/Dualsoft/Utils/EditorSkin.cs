using DevExpress.LookAndFeel;
using DevExpress.XtraBars.Ribbon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Dualsoft
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

        internal static void InitSetting(SkinSvgPalette defaultSkin)
        {
            UserLookAndFeel.Default.StyleChanged += (s, e) =>
            {
                var skin = s as UserLookAndFeel;
                EditorSkin.SetSkin($"{skin.ActiveSkinName};{skin.ActiveSvgPaletteName}");
            };

            if (DSRegistry.GetValue(K.RegSkin) == null)
                UserLookAndFeel.Default.SetSkinStyle(defaultSkin);
            else
            {
                var skin = GetSkin();
                var skinName = skin.Split(';')[0]; 
                var skinPalette= skin.Split(';')[1]; 
                UserLookAndFeel.Default.SetSkinStyle(skinName, skinPalette);
            }

        }

    }
}
