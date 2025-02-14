using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridViewExtension
    {
        /// GridColumnCollection type 을 GridColumn 의 IEnumerable로 반환한다.
        public static IEnumerable<GridColumn> GetColumns(this GridView gv) => gv.Columns.Cast<GridColumn>();
        public static IEnumerable<GridColumn> GetColumns(this GridView gv, IEnumerable<string> columnNames) => columnNames.Select(n => gv.Columns[n]);

        /// <summary>
        /// GridView 에서 선택된 행의 data 목록 반환
        /// </summary>
        public static T GetRow<T>(this GridView gv, int rowHandle) => (T)gv.GetRow(rowHandle);

        /// <summary>
        /// GridView 에서 선택된 행의 index 목록 반환
        /// </summary>
        public static T[] GetSelectedRows<T>(this GridView gridView)
        {
            int[] selectedRows = gridView.GetSelectedRows();
            return selectedRows.Select(i => gridView.GetRow(i)).Cast<T>().ToArray();
        }
        public static void MakeReadOnly(this GridView gridView, bool allowCopy = true)
        {
            // 편집은 불가능하게 설정
            gridView.OptionsBehavior.Editable = false;
            if (allowCopy)
            {
                // 텍스트 선택 및 복사를 허용하는 설정
                gridView.OptionsSelection.EnableAppearanceFocusedCell = true; // 포커스된 셀 강조
                gridView.OptionsSelection.MultiSelect = true; // 여러 셀 선택 가능
                gridView.OptionsClipboard.CopyColumnHeaders = DefaultBoolean.True; // 컬럼 헤더 복사 허용

                // 복사 시 셀 텍스트를 복사하게 설정
                gridView.OptionsClipboard.AllowCopy = DefaultBoolean.True;
            }
        }


        /// <summary>
        /// GridView 에 동적으로 column 추가.
        ///<br/> - 추가할 column 명
        ///<br/> - Item T 가 주어졌을 때, 해당 column 에 생성할 button 이름과
        ///<br/> - 이 button 이 눌렸을 때 수행할 action 을 반환하는 함수를 제공하여야 한다.  T -> (string, Action[T])
        /// </summary>
        /*
         * Sample
         *
            gridView1.AddActionColumn<ORMLoadDeviceInfoRow>("Confirm", row => {
                void confirm(ORMLoadDeviceInfoRow row)
                {
                    //if (!Items.Remove(row))
                    //    DcDebug.BreakOnce();
                    gridView1.RefreshData();
                }
                return ("Confirm", confirm);
                return (null, null);        // 이 경우, action button 생성하지 않음.
            });
         */
        public static void AddActionColumn<T>(this GridView gridView, string newColumnName, Func<T, (string, Action<T>)> buttonCaptionAndActionGetter)
        {
            if (!gridView.OptionsBehavior.Editable)
                throw new Exception("Adding action column only works when gridView.OptionsBehavior.Editable is true!");

            // 1. GridView에 새 컬럼 추가
            var actionColumn = gridView.Columns.AddVisible(newColumnName, newColumnName);
            actionColumn.UnboundType = DevExpress.Data.UnboundColumnType.String;

            // CustomDrawCell 이벤트를 사용해 버튼처럼 보이도록 셀을 그리기
            gridView.CustomDrawCell += (sender, e) =>
            {
                if (e.Column == actionColumn && e.RowHandle >= 0)
                {
                    T rowData = (T)gridView.GetRow(e.RowHandle);
                    var (caption, _) = buttonCaptionAndActionGetter(rowData);
                    if (caption != null)
                    {
                        // 버튼 스타일로 텍스트 그리기
                        var buttonRect = e.Bounds;
                        e.Graphics.FillRectangle(SystemBrushes.Control, buttonRect);
                        e.Graphics.DrawRectangle(Pens.Gray, buttonRect.X + 2, buttonRect.Y + 2, buttonRect.Width - 4, buttonRect.Height - 4);
                        TextRenderer.DrawText(e.Graphics, caption, e.Appearance.Font, buttonRect, System.Drawing.Color.Navy,
                                              TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    e.Handled = true; // DevExpress 기본 렌더링 방지
                }
            };

            // MouseUp 이벤트에서 단일 클릭을 처리하여 버튼 동작 수행
            gridView.GridControl.MouseUp += (s, e) =>
            {
                var hitInfo = gridView.CalcHitInfo(e.Location);
                if (hitInfo.InRowCell && hitInfo.Column == actionColumn)
                {
                    T rowData = (T)gridView.GetRow(hitInfo.RowHandle);
                    var (_, action) = buttonCaptionAndActionGetter(rowData);
                    if (action != null)
                    {
                        action(rowData);
                        gridView.RefreshData();
                    }
                }
            };

            // ShownEditor 이벤트에서 특정 셀이 편집 모드로 진입하지 않도록 방지
            gridView.ShownEditor += (s, e) =>
            {
                if (gridView.FocusedColumn == actionColumn)
                {
                    gridView.CloseEditor(); // 편집 모드를 종료
                }
            };
        }


        /// <summary>
        /// Unbound column 추가.
        /// </summary>
        // https://docs.devexpress.com/WindowsForms/1477/controls-and-libraries/data-grid/unbound-columns
        public static GridColumn AddUnboundColumnCustom<RowT, CellT>(
            this GridView gridView,
            string columnName,
            Func<RowT, CellT> getter,
            Action<RowT, CellT> setter)
            where RowT : class
        {
            // Unbound 열 생성
            GridColumn unboundColumn = gridView.Columns.AddField(columnName);
            unboundColumn.UnboundDataType = typeof(CellT);
            unboundColumn.Visible = true;
            unboundColumn.OptionsColumn.AllowEdit = true;

            // CustomUnboundColumnData 이벤트 핸들러 등록
            gridView.CustomUnboundColumnData += (sender, e) =>
            {
                if (e.Column.FieldName == columnName && e.Row is RowT row)
                {
                    if (e.IsGetData)
                    {
                        // getter 함수 호출로 값을 가져옴
                        e.Value = getter(row);
                    }
                    else if (e.IsSetData)
                    {
                        // setter 함수 호출로 값을 설정
                        if (e.Value is CellT value)
                        {
                            setter(row, value);
                        }
                    }
                }
            };

            return unboundColumn;
        }



    }
}
