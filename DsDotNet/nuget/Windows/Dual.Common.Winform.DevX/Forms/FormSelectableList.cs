using System;
using System.Linq;
using System.Collections.Generic;

namespace Dual.Common.Winform.DevX
{
    public partial class FormSelectableList : DevExpress.XtraEditors.XtraForm
    {
        //public UcSelectableStringList EditableStringList1 => ucEditableStringList1;
        //public IEnumerable<string> Items => ucSelectableStringList1.Items;
        public FormSelectableList()
        {
            InitializeComponent();
        }

        //public FormSelectableList(IEnumerable<string> items, string title=null)
        //    : this()
        //{
        //    ucEditableStringList1.SetItems(items);
        //    Text = title;
        //}

        private void FormSelectableList_Load(object sender, EventArgs args)
        {
            //ucEditableStringList1.ManageControls(btnAdd, btnDelete, textEdit1);
            //btnOK.Click += (s, e) => Close(); // DialogResult = System.Windows.Forms.DialogResult.OK;
            //btnCancel.Click += (s, e) => Close(); // DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}