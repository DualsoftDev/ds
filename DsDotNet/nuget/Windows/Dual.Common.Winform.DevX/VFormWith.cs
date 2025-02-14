using DevExpress.XtraEditors;

namespace Dual.Common.Winform.DevX
{
    /// <summary>
    /// Text memo 를 포함하는 일반적인 (가상의) form
    /// </summary>
    public static class VFormWithMemo
    {
        public static DxFormWithPanel Create(string memo, bool showOkCancel=false)
        {
            var memoEdit = new MemoEdit();
            memoEdit.Text = memo;
            return new DxFormWithPanel(memoEdit, showOkCancel);
        }
    }

    /// <summary>
    /// DevExpress Grid 를 포함하는 일반적인 (가상의) form
    /// </summary>
    public static class VFormWithGrid
    {
        public static DxFormWithPanel Create(object dataSource=null, bool showOkCancel=true)
        {
            var grid = new UcDxGrid();
            grid.DataSource = dataSource;
            return new DxFormWithPanel(grid, showOkCancel);
        }
    }

}
