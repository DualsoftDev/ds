using DevExpress.XtraEditors;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public class UcEditableStringList : XtraUserControl
    {
        private readonly BindingList<string> items = new BindingList<string>();
        public string[] Items => items.ToArray();
        public readonly ListBoxControl ListBox = new ListBoxControl();
        public bool AllowDuplicateItems { get; set; }

        public UcEditableStringList()
        {
            // ListBox 설정
            ListBox.Dock = DockStyle.Fill;
            ListBox.DataSource = items;  // BindingList를 직접 DataSource로 설정
            ListBox.SelectedIndex = -1;  // 초기화 시 SelectedIndex를 -1로 설정
            ListBox.SelectionMode = SelectionMode.MultiExtended; // 다중 선택 모드 설정
            this.Controls.Add(ListBox);
        }

        public UcEditableStringList(IEnumerable<string> newItems) : this()
        {
            SetItems(newItems);
        }

        /// <summary>
        /// List 를 관리하는, 외부에서 생성된 버튼(add/delete)과 텍스트 박스(add 용)를 설정.
        /// </summary>
        public void ManageControls(SimpleButton btnAdd, SimpleButton btnDelete, TextEdit textEdit)
        {
            btnAdd.Enabled = false;
            btnDelete.Enabled = false;
            textEdit.EditValueChanged += (s, e) =>
            {
                var text = (string)textEdit.EditValue;
                btnAdd.Enabled = !string.IsNullOrEmpty(text) && (AllowDuplicateItems || !items.Contains(text));
            };
            btnAdd.Click += (s, e) =>
            {
                AddItem((string)textEdit.EditValue);
                textEdit.EditValue = null;
            };
            btnDelete.Click += (s, e) => DeleteSelectedItems();
            ListBox.SelectedIndexChanged += (s, e) =>
            {
                btnDelete.Enabled = ListBox.SelectedIndices.Any();
            };
        }

        public void AddItem(string item) => items.Add(item);
        public void DeleteSelectedItems()
        {
            // 선택된 항목을 리스트로 변환 후, 각 항목을 items에서 제거
            foreach (var selectedItem in ListBox.SelectedItems.Cast<string>().ToList())
            {
                items.Remove(selectedItem);
            }
            ListBox.SelectedIndex = -1; // 선택 상태 초기화
        }

        // 외부에서 항목 리스트를 설정하는 메서드
        public void SetItems(IEnumerable<string> newItems)
        {
            ListBox.DataSource = null; // DataSource 해제
            items.Clear();
            foreach (var item in newItems)
                items.Add(item);

            ListBox.DataSource = items; // DataSource 재설정
            ListBox.SelectedIndex = -1; // 선택 상태 초기화
        }

        // 현재 항목 리스트를 가져오는 메서드
        public string[] GetItems()
        {
            return items.ToArray();
        }
    }
}
