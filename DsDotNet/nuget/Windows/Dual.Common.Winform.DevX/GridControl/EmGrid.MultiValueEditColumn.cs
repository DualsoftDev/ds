using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridControlExtension
    {
        static void editCell<T>(this GridColumn gridColumn, Func<T, string[], bool> editAction) where T : class // 'class' 제약 추가
        {
            GridView gridView = gridColumn.View as GridView;
            var row = gridView.GetRow(gridView.FocusedRowHandle) as T;
            if (row != null && editAction(row, getValuesFromRow(row, gridColumn)))
            {
                gridView.RefreshRow(gridView.FocusedRowHandle); // 변경 사항 갱신
            }
        }

        /// <summary>
        /// GridColumn 객체에 항목 추가/삭제 기능을 추가.
        /// <br/> - Column display 는 콤마로 구분하여 표시
        /// <br/> - editAction 을 통해서
        /// </summary>
        public static void MakeMultiValueEdit<T>(
            this GridColumn gridColumn,
            Func<T, string[], bool> editAction) where T : class // 'class' 제약 추가
        {
            GridView gridView = gridColumn.GetGridView();
            RepositoryItemButtonEdit buttonEdit = new RepositoryItemButtonEdit();
            var repo = gridView.GridControl.RepositoryItems;
            if (!repo.Contains(buttonEdit))
                repo.Add(buttonEdit);
            gridColumn.ColumnEdit = buttonEdit;

            gridColumn.MakeReadOnly();      // grid 상에서 직접 편집행위 금지
            gridView.RowCellClick += (s, e) =>
            {
                if (e.Column == gridColumn)
                    gridColumn.editCell(editAction);
            };

            // 클릭 시 버튼이 바로 눌리도록 설정
            gridView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;
            // FocusedRowChanged 이벤트에서 자동으로 버튼 편집기 활성화
            gridView.FocusedRowChanged += (s, e) => openButtonEditForCell(gridView, gridColumn);
            gridView.FocusedColumnChanged += (s, e) => openButtonEditForCell(gridView, gridColumn);

            // 버튼 클릭 시 편집 창을 열어 배열 편집
            buttonEdit.ButtonClick += (s, e) => gridColumn.editCell(editAction);

            // CustomDisplayText 이벤트로 배열을 ,로 구분하여 한 셀에 표시
            gridView.CustomColumnDisplayText += (s, e) =>
            {
                if (e.Column == gridColumn)
                {
                    var row = gridView.GetRow(e.ListSourceRowIndex) as T;
                    if (row != null)
                    {
                        var values = getValuesFromRow(row, gridColumn);
                        e.DisplayText = string.Join(", ", values);
                    }
                }
            };

        }

        // 셀을 클릭할 때 자동으로 버튼 편집기를 열도록 하는 메서드
        static void openButtonEditForCell(GridView gridView, GridColumn gridColumn)
        {
            if (gridView.FocusedColumn.FieldName == gridColumn.FieldName)
            {
                gridView.ShowEditor();  // 셀 편집기 열기
                if (gridView.ActiveEditor is ButtonEdit buttonEdit && buttonEdit.Properties.Buttons.Count > 0)
                {
                    buttonEdit.PerformClick(buttonEdit.Properties.Buttons[0]);
                }
            }
        }

        static string[] getValuesFromRow<T>(T row, GridColumn gridColumn)
        {
            // Row의 특정 필드를 기반으로 값을 가져오는 로직 구현 (예: reflection 또는 직접 필드 접근)
            PropertyInfo fieldProperty = typeof(T).GetProperty(gridColumn.FieldName);
            if (fieldProperty != null && fieldProperty.GetValue(row) is IEnumerable<string> namesArray)
            {
                return namesArray.ToArray();
            }
            return new string[]{};
        }




        /// <summary>
        /// GridColumn 객체에 다중 선택 기능을 추가.
        /// <br/> - availableOptions 중에서 선택된 값만 저장
        /// <br/> - Column display 는 콤마로 구분하여 표시
        /// <br/> - editAction 에서 check 선택된 항목들을 Row 데이터에 반영
        /// </summary>
        public static void MakeMultiCheckEdit<T>(
            this GridColumn gridColumn,
            IEnumerable<string> availableOptions,
            Func<T, string[], bool> editAction,
            Action<T, RepositoryItemCheckedComboBoxEdit, CheckedComboBoxEdit> shownEditor=null) where T : class
        {
            var gridView = gridColumn.View as GridView;
            var gridControl = gridView.GridControl;

            // 클릭 시 버튼이 바로 눌리도록 설정
            gridView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;

            // CheckedComboBoxEdit 설정 및 RepositoryItems에 추가
            var checkedComboBox = new RepositoryItemCheckedComboBoxEdit() { EditValueType = EditValueTypeCollection.List };
            if (!gridControl.RepositoryItems.Contains(checkedComboBox))
                gridControl.RepositoryItems.Add(checkedComboBox);

            // 가능한 옵션 추가
            foreach (var option in availableOptions)
                checkedComboBox.Items.Add(option);

            gridColumn.ColumnEdit = checkedComboBox;

            // ShowingEditor 이벤트를 사용하여 초기 체크 상태 설정
            // Data 에 이미 존재하는 항목들에 대해서 check 상태를 설정
            gridView.ShownEditor += (s, e) =>
            {
                if (gridView.FocusedColumn == gridColumn && gridView.ActiveEditor is CheckedComboBoxEdit editor)
                {
                    var row = gridView.GetFocusedRow() as T;
                    if (row != null)
                    {
                        if (shownEditor != null)
                            shownEditor(row, checkedComboBox, editor);
                        else
                        {
                            var values = typeof(T).GetProperty(gridColumn.FieldName)?.GetValue(row) as IEnumerable<string>;
                            if (values != null)
                            {
                                // EditValue를 통해 초기 체크 상태 설정
                                editor.EditValue = values;
                            }
                        }
                    }
                }
            };
            // CustomDisplayText 이벤트로 선택된 항목을 콤마로 구분하여 표시
            gridView.CustomColumnDisplayText += (s, e) =>
            {
                if (e.Column == gridColumn)
                {
                    e.DisplayText = (e.Value == null) ? "" : string.Join(", ", (string[])e.Value);
                }
            };


            // 편집 완료 후 데이터 반영.  (editAction 호출을 통해)
            checkedComboBox.Closed += (sender, e) =>
            {
                var editor = sender as CheckedComboBoxEdit;
                if (gridView.FocusedRowHandle >= 0 && editor != null)
                {
                    var row = gridView.GetRow(gridView.FocusedRowHandle) as T;
                    if (row != null)
                    {
                        var selectedValues = editor.Properties.Items
                            .GetCheckedValues()
                            .Select(val => val.ToString())
                            .ToArray();

                        // editAction을 통해 데이터 업데이트
                        editAction(row, selectedValues);
                        gridView.RefreshRow(gridView.FocusedRowHandle);
                    }
                }
            };
        }


        /// <summary>
        /// GridColumn 객체에 문자열 옵션 단일 선택 기능을 추가.
        /// <br/> - availableOptions 중 하나의 값을 선택하여 저장
        /// <br/> - Column display 는 선택된 값만 표시
        /// <br/> - editAction 에서 선택된 항목을 Row 데이터에 반영
        /// </summary>
        public static void MakeSingleSelectEdit<T>(
            this GridColumn gridColumn,
            IEnumerable<string> availableOptions,
            Func<T, string, bool> editAction,
            Action<RepositoryItemComboBox> showingEditor = null,
            Action<T, RepositoryItemComboBox, ComboBoxEdit> shownEditor = null
            ) where T : class
        {
            var gridView = gridColumn.View as GridView;
            var gridControl = gridView.GridControl;

            // 클릭 시 버튼이 바로 눌리도록 설정
            gridView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;

            // ComboBoxEdit 설정 및 RepositoryItems에 추가
            var comboBox = new RepositoryItemComboBox();
            if (!gridControl.RepositoryItems.Contains(comboBox))
                gridControl.RepositoryItems.Add(comboBox);

            // 가능한 옵션 추가
            foreach (var option in availableOptions)
                comboBox.Items.Add(option);

            gridColumn.ColumnEdit = comboBox;

            if (showingEditor != null)
            {
                gridView.ShowingEditor += (s, e) => {
                    showingEditor(comboBox);
                };
            }

            // ShowingEditor 이벤트를 사용하여 초기 선택 상태 설정
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
                            var value = typeof(T).GetProperty(gridColumn.FieldName)?.GetValue(row) as string;
                            if (value != null)
                            {
                                // EditValue를 통해 초기 선택 상태 설정
                                editor.EditValue = value;
                            }
                        }
                    }
                }
            };

            // 편집 완료 후 데이터 반영.  (editAction 호출을 통해)
            comboBox.SelectedIndexChanged += (sender, e) =>
            {
                var editor = sender as ComboBoxEdit;
                if (gridView.FocusedRowHandle >= 0 && editor != null)
                {
                    var row = gridView.GetRow(gridView.FocusedRowHandle) as T;
                    if (row != null)
                    {
                        var selectedValue = editor.EditValue as string;

                        // editAction을 통해 데이터 업데이트
                        editAction(row, selectedValue);
                        gridView.RefreshRow(gridView.FocusedRowHandle);
                    }
                }
            };
        }
    }
}
