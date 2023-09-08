using DevExpress.XtraEditors;
using Dual.Common.Core;

namespace DSModeler
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public partial class ItemDetailPage : XtraUserControl
    {
        public ItemDetailPage(DsHMIDataCommon item)
        {
            InitializeComponent();
            labelTitle.Text = item.Title;
            labelSubtitle.Text = item.Subtitle;
            if (item.Image != null)
                imageControl.Image = item.Image;
            labelContent.Text = item.Subtitle;
        }
    }
}
