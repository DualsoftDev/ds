using DevExpress.Utils;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;

using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System.Drawing;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridControlExtension
    {
        public static Color ReadOnlyColor { get; set; } = Color.AliceBlue;

        /// Grid view 의 주어진 column 에 checkedit 을 적용한다.
        public static RepositoryItem ApplyCheckEditOnColumn(this GridView gv, string columnName) => gv.ApplyCheckEditOnColumns(new[] { columnName });

        /// Grid view 의 주어진 column 들에 checkedit 을 적용한다.
        public static RepositoryItem ApplyCheckEditOnColumns(this GridView gv, IEnumerable<string> columnNames)
        {
            RepositoryItemCheckEdit checkEdit = null;
            foreach (var cn in columnNames)
                checkEdit = gv.Columns[cn].ApplyImmediateCheckBox();

            return checkEdit;
        }


        public static object GetCellData(this GridView gv, int rowHandle, GridColumn column) => gv.GetRowCellValue(rowHandle, column);
        public static object GetCellData(this GridView gv, int rowHandle, string column) => gv.GetRowCellValue(rowHandle, column);


        public static void ChangeBackColorOfReadOnlyColumns(this GridView gv) => gv.ChangeBackColorOfReadOnlyColumns(ReadOnlyColor);
        public static void ChangeBackColorOfReadOnlyColumns(this GridView gv, Color backColor)
        {
            IEnumerable<GridColumn> targetColumns = gv.GetColumns();
            if (!gv.GridControl.AllowDrop)
                targetColumns = targetColumns.Where(col => col.ReadOnly || !col.OptionsColumn.AllowEdit);

            foreach (var col in targetColumns)
                col.AppearanceCell.BackColor = backColor;

            foreach (var col in gv.GetColumns().Except(targetColumns))
                col.AppearanceCell.BackColor = Color.White;
        }

        public static void HideGroupPanel(this GridView gv) => gv.OptionsView.ShowGroupPanel = false;
        public static void ShowGroupPanel(this GridView gv) => gv.OptionsView.ShowGroupPanel = true;

        public class GridCellInfo
        {
            public object Data { get; }
            public int RowHandle { get; }
            public GridColumn GridColumn { get; }
            public GridCellInfo(object data, int rowHandle, GridColumn gridColumn)
            {
                Data = data;
                RowHandle = rowHandle;
                GridColumn = gridColumn;
            }
        }
        public static GridCellInfo FindClickedCellInfo(this GridView gv, EventArgs args)
        {
            DXMouseEventArgs ea = args as DXMouseEventArgs;
            GridHitInfo info = gv.CalcHitInfo(ea.Location);
            if (info.InRow || info.InRowCell)
            {
                var data = gv.GetRow(info.RowHandle);
                return new GridCellInfo(data, info.RowHandle, info.Column);
            }

            return null;
        }

    }
}
