using DevExpress.XtraBars.Navigation;
using System.Drawing;

namespace DSModeler.Tree
{
    public static class SimTree
    {



        public static void SimPlayUI(AccordionControlElement ace_Play, bool bOn)
        {
            ace_Play.Appearance.Default.BackColor = Color.RoyalBlue;
            ace_Play.Appearance.Default.Options.UseBackColor = bOn;
        }

    }

}


