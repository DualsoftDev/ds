using DevExpress.Utils.Filtering;
using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTable : XtraUserControl
    {
        public UcDsTable()
        {
            InitializeComponent();
            gridView1.OptionsView.ShowAutoFilterRow = true; // Enable filter row
            gridView1.OptionsView.ShowGroupPanel = false; // Disable group panel
            gridView1.OptionsBehavior.Editable = false; // Read-only grid
            gridView1.OptionsView.EnableAppearanceEvenRow = true;   
        }

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            try
            {
                gridControl1.DataSource = opcTagManager.OpcTags;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
