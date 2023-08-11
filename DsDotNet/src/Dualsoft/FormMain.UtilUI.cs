using DevExpress.XtraEditors;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dual.Common.Core;
using Dual.Common.Winform;
using static Model.Import.Office.ImportPPTModule;

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