using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

using System;
using System.Linq;
using System.Collections.Generic;
using Dual.Common.Base.CS;
using DevExpress.Utils.Html.Internal;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridColumnExtension
    {
        const string ELLIPSIS = "...";
        static string join(IEnumerable<string> values) => string.Join(", ", values);

        /// <summary>
        /// string[] 를 갖는 GridColumn 에 대해서, "..." 을 통해 문자열을 추가 하거나, 기존 문자열을 선택해서 string[] 를 변경할 수 있는 column 으로 설정
        /// <br/> - ComboBoxEdit 을 적용하고, 콤보박스의 선택값을
        /// <br/>   * string[] 의 맨 처음 인덱스로 변경하거나,
        /// <br/>   * 하나만 선택하고 나머지 삭제 등의 편집 가능
        /// </summary>
        public static void MakeComboWithMoreOption<T>(
            this GridColumn gridColumn,
            Func<T, string[]> getValuesFromRow,
            Func<T, string, bool> editAction,
            Func<T, string[], string[]> getMoreValues,
            Action<T, RepositoryItemComboBox, ComboBoxEdit> shownEditor = null) where T : class
        {
            string toString(object value) => value is IEnumerable<string> vs ? join(vs) : value as string;

            var gridView = gridColumn.View as GridView;
            var gridControl = gridView.GridControl;

            var comboBox = new RepositoryItemComboBox();

            if (!gridControl.RepositoryItems.Contains(comboBox))
                gridControl.RepositoryItems.Add(comboBox);

            gridColumn.ColumnEdit = comboBox;

            gridView.ShownEditor += (s, e) =>
            {
                if (gridView.FocusedColumn == gridColumn && gridView.ActiveEditor is ComboBoxEdit editor)
                {
                    var row = gridView.GetFocusedRow() as T;
                    if (row != null)
                    {
                        if (shownEditor != null)
                            shownEditor(row, comboBox, editor);
                        else
                        {
                            var values = getValuesFromRow(row);
                            if (values != null)
                            {
                                editor.EditValue = join(values);

                                var comboBox = editor.Properties as RepositoryItemComboBox;
                                comboBox.Items.Clear();
                                foreach (var value in values)
                                    comboBox.Items.Add(value);

                                comboBox.Items.Add(ELLIPSIS);
                            }

                            // 새로운 데이터 초기화 코드
                            editor.EditValue = values ?? Array.Empty<string>();
                        }
                    }
                }
            };

            // CustomDisplayText 이벤트 사용하여 string[]의 값을 콤마로 구분된 문자열로 표시
            // ComboBoxEdit의 DisplayText를 수정하여 배열 값 표시 문제 해결
            gridView.CustomColumnDisplayText += (s, e) =>
            {
                if (e.Column == gridColumn)
                    e.DisplayText = toString(e.Value);
            };


            gridView.ValidatingEditor += (s, e) =>
            {
                if (gridView.FocusedColumn == gridColumn)
                {
                    var row = gridView.GetFocusedRow() as T;
                    if (row != null)
                        editAction(row, toString(e.Value));
                }
            };


            comboBox.SelectedIndexChanged += (sender, e) =>
            {
                var editor = sender as ComboBoxEdit;
                var rh = gridView.FocusedRowHandle;
                if (rh >= 0 && editor != null)
                {
                    var row = gridView.GetRow(rh) as T;
                    if (row != null)
                    {
                        var selectedValue = editor.EditValue as string;

                        if (selectedValue == ELLIPSIS)
                        {
                            var additionalValues = getMoreValues(row, getValuesFromRow(row));
                            if (additionalValues.NonNullAny())
                            {
                                comboBox.Items.Clear();
                                foreach (var option in getValuesFromRow(row).Concat(additionalValues))
                                    comboBox.Items.Add(option);
                                comboBox.Items.Add(ELLIPSIS);

                                gridView.FocusedColumn = gridColumn;
                                gridView.ShowEditor();
                            }
                            else
                            {
                                // ELLIPSIS 선택으로 인한 새로운 설정 값이 없는 경우, 기존 값을 유지
                                //gridView.RefreshRow(rh);
                            }
                        }
                        else
                        {
                            editAction(row, selectedValue);
                            //gridView.RefreshRow(rh);
                        }

                        gridView.RefreshRow(rh);
                    }
                }
            };
        }
    }
}
