using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public class UcSelectableStringList : XtraUserControl
    {
        private readonly CheckedComboBoxEdit checkedComboBox = new CheckedComboBoxEdit();

        public IEnumerable<string> SelectedItems
        {
            get => checkedComboBox.Properties.Items
                                       .GetCheckedValues()
                                       .Cast<string>();
            set
            {
                foreach (CheckedListBoxItem item in checkedComboBox.Properties.Items)
                {
                    item.CheckState = value.Contains(item.Value.ToString()) ? CheckState.Checked : CheckState.Unchecked;
                }
            }
        }

        public UcSelectableStringList()
        {
            // CheckedComboBoxEdit 설정
            checkedComboBox.Dock = DockStyle.Fill;
            checkedComboBox.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            this.Controls.Add(checkedComboBox);
        }

        public UcSelectableStringList(IEnumerable<string> items, IEnumerable<string> selectedItems) : this()
        {
            SetItems(items);
            SelectedItems = selectedItems;
        }

        // 항목 리스트를 설정하는 메서드
        public void SetItems(IEnumerable<string> items)
        {
            checkedComboBox.Properties.Items.Clear();
            foreach (var item in items)
            {
                checkedComboBox.Properties.Items.Add(item, false);
            }
        }

        // 선택된 항목 리스트를 가져오는 메서드
        public IEnumerable<string> GetSelectedItems()
        {
            return checkedComboBox.Properties.Items.GetCheckedValues().Cast<string>();
        }
    }
}
