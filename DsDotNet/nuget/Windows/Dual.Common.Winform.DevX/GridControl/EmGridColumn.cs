using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

using Dual.Common.Base.FS;

using static DevExpress.Utils.Svg.CommonSvgImages;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridColumnExtension
    {
        public static GridView GetGridView(this GridColumn gridColumn) => gridColumn.View as GridView;
        public static GridControl GetGridControl(this GridColumn gridColumn) => gridColumn.View?.GridControl;
        public static T ApplyEditor<T>(this GridColumn column) where T : RepositoryItem, new()
        {
            GridControl gridControl = column.GetGridControl();
            return
                gridControl.AddRepositoryItemOnDemand<T>()
                .Tee(r =>
                {
                    // GridControl 컬럼에 ColumnEdit으로 설정
                    column.ColumnEdit = r;
                });
        }

        /// <summary>
        /// Grid column 의 data 변경을 즉시 반영하도록 설정 (해당 cell 을 떠나지 않더라도 반영)
        /// <br/> T : RepositoryItemCheckEdit, RepositoryItemComboBox, ...
        /// </summary>
        public static T ApplyImmediate<T>(this GridColumn column) where T : RepositoryItem, new()
        {
            return
                column.ApplyEditor<T>()
                .Tee(r =>
                {
                    r.EditValueChanged += (s, e) => column.View.PostEditor();  // 체크박스 값 즉시 반영
                });
        }

        /// <summary>
        /// Grid column 의 check box를 즉시 반영하도록 설정 (해당 cell 을 떠나지 않더라도 반영)
        /// </summary>
        public static RepositoryItemCheckEdit ApplyImmediateCheckBox(this GridColumn column) =>
            ApplyImmediate<RepositoryItemCheckEdit>(column);






        public static RepositoryItemComboBox ApplyImmediateComboBox(this GridView gv, string columnName)
            => gv.Columns[columnName].ApplyImmediateComboBox();
        public static RepositoryItemComboBox ApplyImmediateComboBox(this GridColumn column)
        {
            GridView gridView = column.View as GridView;
            var comboEdit = ApplyImmediate<RepositoryItemComboBox>(column);




            //GridControl gridcontrol = gridView.GridControl;
            //// https://www.devexpress.com/Support/Center/Question/Details/T164030/grid-how-to-show-yes-no-instead-of-check-boxes-in-a-boolean-column
            //var comboEdit = (RepositoryItemComboBox)gridcontrol.RepositoryItems.Add("ComboBoxEdit");
            //column.ColumnEdit = comboEdit;
            comboEdit.SelectedIndexChanged += (s, e) => {
                Trace.WriteLine("SelectedIndexChanged triggered");
                gridView.PostEditor();    // 값이 변경될 때 바로 반영되도록 설정
            };
            //comboEdit.EditValueChanged += (s, e) => {
            //    Trace.WriteLine("EditValueChanged triggered");
            //    gridView.PostEditor();    // 값이 변경될 때 바로 반영되도록 설정
            //};


            // CellValueChanged 이벤트를 통해 변경 사항을 감지하고 반영
            gridView.CellValueChanged += (s, e) =>
            {
                var columnName = column.FieldName;
                if (e.Column.FieldName == columnName)
                {
                    Trace.WriteLine($"Cell value changed in column '{columnName}'");
                    // 바로 반영
                    gridView.PostEditor();    // 값이 변경될 때 바로 반영되도록 설정
                }
            };
            return comboEdit;
        }



        public static void MakeReadOnly(this GridColumn gridColumn)
        {
            gridColumn.OptionsColumn.AllowEdit = false; // 읽기 전용으로 설정
            gridColumn.OptionsColumn.ReadOnly = true;   // UI 표시에도 읽기 전용임을 명확하게 하기 위해 설정 (선택 사항)
        }


        /* GridColumn 전체가 아닌, Column 내의 특정 Cell 에만 readonly 적용 */
        static HashSet<GridColumn> _readOnlyMarkedColumns = new HashSet<GridColumn>();

        /// <summary>
        /// GridColumn 전체가 아닌, Column 내의 특정 Cell 에만 readonly 적용
        /// </summary>
        public static void MakeReadOnly(this GridColumn gridColumn, Func<int, GridColumn, bool> cellReadOnlyPredicate )
        {
            if (! _readOnlyMarkedColumns.Contains(gridColumn))
            {
                _readOnlyMarkedColumns.Add(gridColumn);
                GridView view = gridColumn.GetGridView();
                view.ShowingEditor += (s, e) =>
                {
                    if (view.FocusedColumn == gridColumn) // 특정 컬럼
                    {
                        var rowHandle = view.FocusedRowHandle;

                        // 조건: 편집을 막고 싶은 경우
                        if (cellReadOnlyPredicate(rowHandle, view.FocusedColumn))
                            e.Cancel = true; // 편집 막기
                    }

                };
                view.RowCellStyle += (s, e) =>
                {
                    if (cellReadOnlyPredicate(e.RowHandle, e.Column))
                    {
                        if (e.Column == gridColumn) // 해당 cell (해당 row, 해당 column) 에만 style 적용
                            e.Appearance.BackColor = GridControlExtension.ReadOnlyColor;
                    }
                };
            }
        }
    }
}
