using System.Drawing;
using System.Windows.Forms;

using DevExpress.XtraEditors;

namespace Dual.Common.Winform.DevX
{
    public static class ButtonExtension
    {
        // { button 활성화시 색상 변경: Simple code
        public static Color EnabledColor { get; set; } = Color.LightCyan;
        public static Color DisabledColor { get; set; } = Color.LightGray;
        public static void SetEnabledSimpleButton(this SimpleButton simpleButton, bool enable)
        {
            simpleButton.Enabled = enable;
            simpleButton.MarkUseBackColor();
        }

        public static void MarkUseBackColor(this SimpleButton simpleButton)
        {
            simpleButton.Appearance.Options.UseBackColor = true;
            simpleButton.Appearance.Options.UseBorderColor = true;
            simpleButton.Appearance.Options.UseForeColor = true;
            bool enable = simpleButton.Enabled;
            simpleButton.Appearance.BackColor = enable ? EnabledColor : DisabledColor;
            //simpleButton.Appearance.ForeColor = enable ? EnabledColor : DisabledColor;
            //simpleButton.Appearance.BorderColor = enable ? EnabledColor : DisabledColor;
        }


        /// <summary>
        /// SimpleButton's SetEnabled
        /// </summary>
        public static void SetEnabled(this SimpleButton simpleButton, bool enable) => SetEnabledSimpleButton(simpleButton, enable);


        /// <summary>
        /// SimpleButton's Enable
        /// </summary>
        public static void Enable(this SimpleButton simpleButton) => simpleButton.SetEnabled(true);
        /// <summary>
        /// SimpleButton's Disable
        /// </summary>
        public static void Disable(this SimpleButton simpleButton) => simpleButton.SetEnabled(false);
        // } button 활성화시 색상 변경
    }


    public static class CheckEditExtension
    {
        public static void MakeTriState(this CheckEdit checkEdit)
        {
            checkEdit.Properties.AllowGrayed = true;    // 삼중 상태 허용
            checkEdit.CheckState = CheckState.Indeterminate; // 초기 상태를 Indeterminate로 설정
        }
        public static void SetState(this CheckEdit checkEdit, bool? state)
        {
            switch(state)
            {
                case null : checkEdit.CheckState = CheckState.Indeterminate; break;
                case true : checkEdit.CheckState = CheckState.Checked; break;
                case false: checkEdit.CheckState = CheckState.Unchecked; break;
            }
        }
        public static bool IsChecked(this CheckEdit checkEdit) => checkEdit.CheckState == CheckState.Checked;
        public static bool IsUnchecked(this CheckEdit checkEdit) => checkEdit.CheckState == CheckState.Unchecked;
        public static bool IsIndeterminate(this CheckEdit checkEdit) => checkEdit.CheckState == CheckState.Indeterminate;

    }
}
