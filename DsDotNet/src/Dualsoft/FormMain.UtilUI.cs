using Dual.Common.Core;
using Dual.Common.Winform;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {

        internal void UpdateProcessUI(int uIDisplay)
        {
            this.Do(() =>
            {
                barEditItem_Process.EditValue = uIDisplay;
            });
        }


    }
}