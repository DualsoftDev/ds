using System;
using System.Linq;
using System.Collections.Generic;

namespace Dual.Common.Winform.DevX
{
    public partial class FormEditableList : DevExpress.XtraEditors.XtraForm
    {
        public UcEditableStringList EditableStringList1 => ucEditableStringList1;
        public string[] Items => ucEditableStringList1.Items;
        public FormEditableList()
        {
            InitializeComponent();
        }

        public FormEditableList(IEnumerable<string> items, string title=null)
            : this()
        {
            ucEditableStringList1.SetItems(items);
            Text = title;
        }

        private void FormEditableList_Load(object sender, EventArgs args)
        {
            ucEditableStringList1.ManageControls(btnAdd, btnDelete, textEdit1);
            btnOK.Click += (s, e) => Close(); // DialogResult = System.Windows.Forms.DialogResult.OK;
            btnCancel.Click += (s, e) => Close(); // DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}