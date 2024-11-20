using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace OPC.UA.DSClient.Winform
{
    public partial class TagViewerForm : Form
    {
        private readonly DataTable m_tagDataTable;

        public TagViewerForm(DataTable tagDataTable)
        {
            InitializeComponent();
            m_tagDataTable = tagDataTable;

            // Initialize DevExpress GridControl
            InitializeGridControl();
        }

        private void InitializeGridControl()
        {
            gridControl.DataSource = m_tagDataTable;
            // Configure columns
            ConfigureGridColumns();
        }

        private void ConfigureGridColumns()
        {
            // Configure each column to support filtering and adjust settings
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in gridView.Columns)
            {
                column.OptionsFilter.AutoFilterCondition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Contains;

                // Check if the column is a DateTime column
                if (column.FieldName == "Timestamp" && column.ColumnType == typeof(DateTime))
                {
                    // Set display format for DateTime
                    column.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    column.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss"; // Full date and time format
                }

                column.BestFit(); // Adjust column width to content
            }

            gridView.BestFitColumns(); // Adjust all columns
        }


        /// <summary>
        /// Updates the DataTable in a thread-safe manner.
        /// </summary>
        public void UpdateData(string tagName, object value, DateTime timestamp)
        {
            // Ensure thread-safe execution
            if (gridControl.InvokeRequired)
            {
                gridControl.Invoke(new Action(() => UpdateData(tagName, value, timestamp)));
                return;
            }

            // Find the row with the matching Tag Name
            var row = m_tagDataTable.Select($"[Tag Name] = '{tagName}'").FirstOrDefault();
            if (row != null)
            {
                // Update existing row
                row["Value"] = value;
                row["Timestamp"] = timestamp;
            }
            else
            {
                // Add new row if not found
                m_tagDataTable.Rows.Add(tagName, value, value?.GetType().Name ?? "Unknown", timestamp);
            }

            // Refresh the GridControl to reflect changes
            gridControl.RefreshDataSource();
        }
    }
}
