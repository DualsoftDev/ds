using Dual.Common.Core;
using Dual.Common.Winform;

namespace Dualsoft
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