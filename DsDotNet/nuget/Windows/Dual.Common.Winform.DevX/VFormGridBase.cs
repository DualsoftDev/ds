using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using System;

namespace Dual.Common.Winform.DevX
{
    public class VFormGridBase
    {
        public event EventHandler OkClicked;
        public DxFormWithPanel FormWithPanel { get; private set; }
        public UcDxGrid UcGrid { get; private set; }
        public GridControl GridControl => UcGrid.GridControl;
        public GridView GridView => UcGrid.GridView;
        public bool IsDirty => UcGrid.IsDirty;
        public object DataSource {
            get => UcGrid.DataSource;
            set => UcGrid.DataSource = value;
        }

        public VFormGridBase(object dataSource=null, bool showOkCancel=true)
        {
            UcGrid = new UcDxGrid() { DataSource = dataSource };
            FormWithPanel = new DxFormWithPanel(UcGrid, showOkCancel);
            FormWithPanel.OkClicked += (s, e) => OkClicked?.Invoke(this, e); // 이벤트 연결
        }
    }
}
