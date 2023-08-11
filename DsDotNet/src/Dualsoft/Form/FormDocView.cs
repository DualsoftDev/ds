using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

namespace Dualsoft.Form
{
    public partial class FormDocView : DevExpress.XtraEditors.XtraForm
    {
        public FormDocView()
        {
            InitializeComponent();
        }

        public UcView UcView => ucView1;

    }
}