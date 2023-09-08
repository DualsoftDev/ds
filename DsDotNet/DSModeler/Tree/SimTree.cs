using DevExpress.XtraBars.Navigation;
using System.Drawing;
using System.Runtime.Versioning;

namespace DSModeler.Tree
{
    [SupportedOSPlatform("windows")]
    public static class SimTree
    {
        public static void PlayUI(AccordionControlElement ace_Play, bool bOn)
        {
            ace_Play.Appearance.Default.BackColor = Color.RoyalBlue;
            ace_Play.Appearance.Default.Options.UseBackColor = bOn;
        }

    }

}


