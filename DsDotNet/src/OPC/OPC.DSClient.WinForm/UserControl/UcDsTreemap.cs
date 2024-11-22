using DevExpress.XtraEditors;
using DevExpress.XtraTreeMap;
using System;
using System.Data;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTreemap : XtraUserControl
    {
        public UcDsTreemap()
        {
            InitializeComponent();
            InitializeTreeMap();
        }

        private void InitializeTreeMap()
        {
            // Configure treeMapControl1
            treeMapControl1.SelectionMode = ElementSelectionMode.None;

            // Set a colorizer for group differentiation
            var colorizer = new TreeMapPaletteColorizer
            {
                ColorizeGroups = true
            };
            treeMapControl1.Colorizer = colorizer;
        }

        private void SetDataBind(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data available to display.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Configure TreeMapFlatDataAdapter
                var adapter = new TreeMapFlatDataAdapter
                {
                    DataSource = dataTable,
                    LabelDataMember = "Label", // Column for labels
                    ValueDataMember = "Value", // Column for values
                };
                adapter.GroupDataMembers.Add("Group"); // Column for grouping

                treeMapControl1.DataAdapter = adapter;

                // Refresh the TreeMap
                treeMapControl1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        internal void SetDataSource(OpcTagManager opcTagManager)
        {

            try
            {
                // Create a DataTable to map the OpcTagManager data
                var dataTable = new DataTable();
                dataTable.Columns.Add("Label", typeof(string)); // For tag or folder names
                dataTable.Columns.Add("Value", typeof(double)); // For tag values or dummy value for folders
                dataTable.Columns.Add("Group", typeof(string)); // For grouping (ParentPath)

                // Add folder tags
                foreach (var folder in opcTagManager.OpcFolderTags)
                {
                    dataTable.Rows.Add(folder.Name, 0, folder.ParentPath ?? "Root");
                }

                // Add actual tags
                foreach (var tag in opcTagManager.OpcTags)
                {
                    double.TryParse(tag.Value?.ToString(), out double tagValue); // Ensure tag values are numeric
                    dataTable.Rows.Add(tag.Name, tagValue, tag.ParentPath ?? "Root");
                }

                // Bind the DataTable to the TreeMap
                SetDataBind(dataTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
