namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public partial class UcPropertyGrid : DevExpress.XtraEditors.XtraUserControl
    {
        public PropertyGridControl PropertyGrid { get; private set; }
        public UcPropertyGrid()
        {
            InitializeComponent();
            PropertyGrid.Dock = DockStyle.Fill;
            PropertyGrid.DataSourceChanged += (s, e) =>
            {
                object sel = PropertyGrid.SelectedObject ?? (PropertyGrid.SelectedObjects?.FirstOrDefault());
                //if (sel is Record r)
                //{
                //    //Rows["Display"].Visible = false;
                //    PropertyGrid.Rows["InUse"].Visible = false;
                //    ///temp hide
                //    PropertyGrid.Rows["LampColor"].Visible = false;
                //    PropertyGrid.Rows["AlarmColor"].Visible = false;
                //}
                //else if (sel is GraphicFile g)
                //    PropertyGrid.Rows["IsLoadedFromCompiledGraphic"].Visible = false;

                PropertyGrid.BestFit();
            };

            //PropertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            //PropertyGrid.RecordCellStyle += (s, e) => e.Appearance.ForeColor = Color.DimGray;

            //PropertyGrid.BandsInterval = 1;
            //PropertyGrid.Cursor = System.Windows.Forms.Cursors.Default;
            //PropertyGrid.Margin = new System.Windows.Forms.Padding(2);
            //PropertyGrid.OptionsBehavior.Editable = false;
            PropertyGrid.OptionsBehavior.PropertySort = DevExpress.XtraVerticalGrid.PropertySort.NoSort;
            PropertyGrid.OptionsView.AllowReadOnlyRowAppearance = DevExpress.Utils.DefaultBoolean.True;
            PropertyGrid.OptionsView.ShowRootCategories = false;
            PropertyGrid.ActiveViewType = PropertyGridView.Office;

            //PropertyGrid.OptionsView.FixedLineWidth = 1;
            //PropertyGrid.OptionsView.LevelIndent = 0;
            //PropertyGrid.OptionsView.MinRowAutoHeight = 12;
            //PropertyGrid.OptionsView.ShowRootCategories = false;
            //PropertyGrid.ScrollsStyle.ForeColor = System.Drawing.Color.Empty;
        }
    }
}
