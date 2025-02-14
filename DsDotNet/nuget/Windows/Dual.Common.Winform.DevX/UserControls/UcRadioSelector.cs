using DevExpress.XtraEditors.Controls;

using System;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public partial class UcRadioSelector : DevExpress.XtraEditors.XtraUserControl
    {
        public event EventHandler<string> SelectedOptionChanged;
        public UcRadioSelector()
        {
            InitializeComponent();
        }

        private void UcRadioSelector_Load(object sender, EventArgs e)
        {
            radioGroup1.Dock = DockStyle.Fill;
        }

        public string GetSelectedOption()
        {
            if (radioGroup1.SelectedIndex >= 0)
            {
                return radioGroup1.Properties.Items[radioGroup1.SelectedIndex].Description;
            }
            return null; // 선택된 항목이 없는 경우
        }


        // 문자열 배열을 입력으로 받아 RadioGroup의 항목을 설정하는 메서드
        public void SetOptions(string[] options, int selectedIndex=0)
        {
            radioGroup1.Properties.Items.Clear(); // 기존 항목 제거
            for (int i = 0; i < options.Length; i++)
            {
                radioGroup1.Properties.Items.Add(new RadioGroupItem(i, options[i]));
            }

            radioGroup1.SelectedIndex = selectedIndex;  // -1 이면 선택하지 않음

            // 이벤트 핸들러 설정
            radioGroup1.SelectedIndexChanged += (s, e) =>
            {
                // 선택한 항목이 변경되면 이벤트 발생
                string selectedOption = GetSelectedOption();
                if (selectedOption != null && SelectedOptionChanged != null)
                {
                    SelectedOptionChanged(this, selectedOption);
                }
            };
        }
    }
}
