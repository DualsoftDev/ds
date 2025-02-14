using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Resources;
using System.Globalization;

namespace Dual.Common.Winform
{
    public static partial class EmControl
    {
        static void updateSpecial(this Control child, ResourceManager rm)
        {
            // Dirty hack!!
            // DevExpress CheckEdit 에 대해서 언어 전환이 자동으로 수행되지 않아서, 강제로 update
            var tn = child.GetType().Name;
            if (tn == "CheckEdit")
            {
                var text = rm.GetString($"{child.Name}.Properties.Caption", CultureInfo.CurrentUICulture);
                if (text != null && text != "")
                    child.Text = text;
            }
        }

        public static void UpdateResource<T>(this T topLevelControl) where T : Control
        {
            if (topLevelControl == null) throw new ArgumentNullException(nameof(topLevelControl));

            var crm = new ComponentResourceManager(typeof(T));
            var rm = new ResourceManager(typeof(T));

            // 최상위 컨트롤 (폼/유저컨트롤) 리소스 적용.  "$this" 는 resx 의 키워드
            crm.ApplyResources(topLevelControl, "$this");

            // 하위 컨트롤 리소스 적용
            recurse(topLevelControl);

            Form form = topLevelControl as Form;
            if (form != null)
            {
                // 폼의 메뉴 리소스 적용
                if (form.MainMenuStrip != null)
                    crm.ApplyResources(form.MainMenuStrip, form.MainMenuStrip.Name);
            }

            void recurse(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    crm.ApplyResources(child, child.Name);

                    child.updateSpecial(rm);

                    // 자식 컨트롤도 처리
                    if (child.HasChildren)
                        recurse(child);
                }
            }
        }


        public static void UpdateTextResources<T>(this T topLevelControl) where T : Control
        {
            if (topLevelControl == null) throw new ArgumentNullException(nameof(topLevelControl));

            var rm = new ResourceManager(typeof(T));

            // 최상위 컨트롤 (폼/유저컨트롤)의 Text 속성 업데이트
            var text = rm.GetString("$this.Text", CultureInfo.CurrentUICulture);
            if (!string.IsNullOrEmpty(text))
                topLevelControl.Text = text;

            // 하위 컨트롤 Text 속성 업데이트
            recurse(topLevelControl);

            Form form = topLevelControl as Form;
            if (form != null && form.MainMenuStrip != null)
            {
                // 폼의 메뉴 Text 속성 업데이트
                foreach (ToolStripItem item in form.MainMenuStrip.Items)
                    updateToolStripItemText(item, rm);
            }

            void recurse(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    var childText = rm.GetString($"{child.Name}.Text", CultureInfo.CurrentUICulture);
                    if (!string.IsNullOrEmpty(childText))
                        child.Text = childText;

                    child.updateSpecial(rm);

                    // 자식 컨트롤도 처리
                    if (child.HasChildren)
                        recurse(child);
                }
            }

            void updateToolStripItemText(ToolStripItem item, ResourceManager rm)
            {
                var itemText = rm.GetString($"{item.Name}.Text", CultureInfo.CurrentUICulture);
                if (!string.IsNullOrEmpty(itemText))
                    item.Text = itemText;

                // 드롭다운 메뉴 항목 처리
                if (item is ToolStripDropDownItem dropdownItem)
                {
                    foreach (ToolStripItem subItem in dropdownItem.DropDownItems)
                        updateToolStripItemText(subItem, rm);
                }
            }
        }

    }
}

