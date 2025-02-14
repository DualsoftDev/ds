using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

using System;

namespace Dual.Common.Winform.DevX
{
    public class ComboSelector : ComboBoxEdit
    {
        public event EventHandler<string> SelectedOptionChanged;

        public ComboSelector()
        {
            this.Properties.TextEditStyle = TextEditStyles.DisableTextEditor; // 텍스트 입력 불가
            this.SelectedIndexChanged += ComboSelector_SelectedIndexChanged;
        }

        public ComboSelector(string[] options, int selectedIndex = 0)
            : this()
        {
            SetOptions(options, selectedIndex);
        }

        // 문자열 배열을 입력으로 받아 ComboBox의 항목을 설정하는 메서드
        public void SetOptions(string[] options, int selectedIndex = 0)
        { 
            Properties.Items.Clear(); // 기존 항목 제거
            Properties.Items.AddRange(options); // 새 항목 추가

            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                SelectedIndex = selectedIndex;
            }
            else
            {
                SelectedIndex = -1; // 선택하지 않음
            }
        }

        // 현재 선택된 항목을 반환하는 메서드
        public string GetSelectedOption()
        {
            return SelectedItem?.ToString();
        }

        // 선택된 항목이 변경될 때 호출되는 이벤트 핸들러
        private void ComboSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedOption = GetSelectedOption();
            SelectedOptionChanged?.Invoke(this, selectedOption);
        }
    }
}
