// https://www.devexpress.com/Support/Center/Example/Details/E903

using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Drawing;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.ViewInfo;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System.Runtime.InteropServices;

namespace Dual.Common.DevExpressLib
{
    /// <summary>
    /// RepositoryItemButtonEdit 에 text 를 표시하기 위한 helper class.
    /// EmDXGridControl.CreateRepositoryItemButtonEditWithText 를 통해서 사용할 것.
    /// </summary>
    [ComVisible(false)]
    public class RepositoryItemButtonEditWithText : RepositoryItemButtonEdit
    {
        public override BaseEditViewInfo CreateViewInfo()
        {
            return new RepositoryItemButtonEditViewInfo(this);
        }        
    }

    [ComVisible(false)]
    public class RepositoryItemButtonEditViewInfo : ButtonEditViewInfo
    {
        public RepositoryItemButtonEditViewInfo(RepositoryItem item) : base(item) { }

        protected override EditorButtonObjectInfoArgs CreateButtonInfo(EditorButton button, int index)
        {
            return base.CreateButtonInfo(new EditorButtonWithText(), index);
        }
    }

    [ComVisible(false)]
    public class EditorButtonWithText : EditorButton
    {
        public EditorButtonWithText() : this(string.Empty) { }
        public EditorButtonWithText(string myCaption)
        {
            _caption = myCaption;
            Kind = ButtonPredefines.Glyph;
        }
        string _caption = "";
        public override string Caption { get { return _caption; } set { _caption = value; } }
    }


    partial class EmDXGridControl
    {
        public static RepositoryItemButtonEditWithText CreateRepositoryItemButtonEditWithText(this GridControl gridControl, GridView gridView, GridColumn column)
        {
            var ri = new RepositoryItemButtonEditWithText();
            ri.Buttons[0].Kind = ButtonPredefines.Glyph;
            ri.TextEditStyle = TextEditStyles.HideTextEditor;
            gridControl.RepositoryItems.Add(ri);
            column.ColumnEdit = ri;

            /*
             * Sample processing
             */
            //ri.ButtonClick += (sender, e) =>
            //{
            //    System.Windows.Forms.MessageBox.Show(e.Button.Caption);
            //};


            gridView.CustomDrawCell += (sender, e) =>
            {
                if (e.Column == column )
                {
                    ButtonEditViewInfo editInfo = (ButtonEditViewInfo)((GridCellInfo)e.Cell).ViewInfo;
                    editInfo.RightButtons[0].Button.Caption = e.DisplayText;
                }
            };

            gridView.ShownEditor += (sender, e) =>
            {
                GridView view = (GridView)sender;
                if (view.FocusedColumn == column)
                {
                    ButtonEdit ed = (ButtonEdit)view.ActiveEditor;
                    ed.Properties.Buttons[0].Caption = view.GetFocusedDisplayText();
                }
            };

            return ri;
        }
    }
}
