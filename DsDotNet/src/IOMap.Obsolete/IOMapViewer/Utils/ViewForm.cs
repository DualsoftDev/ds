using static IOMapApi.MemoryIOApi;

namespace IOMapViewer;

public partial class ViewForm : XtraForm
{
    public ViewForm()
    {
        InitializeComponent();
    }

    private void ViewForm_Load(object sender, EventArgs e)
    {
        //MemoryIOManager.Delete("M");
        //MemoryIOManager.Create("M", 1024 );

        MemoryIO m = new("M");

        var data2 = m.GetMemoryAsDataTable();

        gridControl1.DataSource = data2;
    }
}