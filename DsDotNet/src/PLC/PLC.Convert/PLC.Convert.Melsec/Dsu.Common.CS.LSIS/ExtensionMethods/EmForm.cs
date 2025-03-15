using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Windows.Forms;

namespace Dsu.Common.CS.LSIS.ExtensionMethods
{
    /// <summary>
    /// 범용 Window Form 관련 extension methods
    /// </summary>
    /// @ingroup groupCommon
    public static class EmForm
    {
        /// <summary>
        /// http://stackoverflow.com/questions/301678/embedding-a-winform-within-a-winform-c
        /// </summary>
        /// <param name="frm">embedding 시킬 form</param>
        /// <param name="ctl">form 이 embedding 될 container control</param>
        /// <param name="fill"></param>
        public static void EmbedToControl(this Form frm, Control ctl, bool fill = true)
        {
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            if (fill)
                frm.Dock = DockStyle.Fill;
            frm.Visible = true;
            ctl.Controls.Add(frm);
        }

        public static void EmbedToControl(this Control inner, Control container, bool fill = true)
        {
            if (fill)
                inner.Dock = DockStyle.Fill;
            inner.Visible = true;
            container.Controls.Add(inner);
        }

        public static bool FullSize(this Form form)
        {
            Contract.Requires(form != null);
            if (form.ParentForm == null)
                return false;
            form.ParentForm.TopMost = true;
            form.ParentForm.FormBorderStyle = FormBorderStyle.None;
            form.ParentForm.WindowState = FormWindowState.Maximized;
            form.WindowState = FormWindowState.Maximized;
            return true;
        }

        public static bool NormalSize(this Form form)
        {
            Contract.Requires(form != null);
            if (form.ParentForm == null)
                return false;
            form.ParentForm.TopMost = true;
            form.ParentForm.FormBorderStyle = FormBorderStyle.Sizable;
            form.ParentForm.WindowState = FormWindowState.Normal;
            form.WindowState = FormWindowState.Normal;
            return true;
        }


        public static void SetLanguage(this Form form, SupportedCultures lang)
        {
            SetLanguage(form, lang.ConvertToString());
        }
        public static void SetLanguage(this Form form, string lang)
        {
            var ci = new CultureInfo(lang);
            ci.Apply();

            ComponentResourceManager rm = new ComponentResourceManager(form.GetType());
            ApplyResourceToControl(rm, form, ci);
            rm.ApplyResources(form, "$this", ci);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/6980888/localization-at-runtime
        /// </summary>
        /// <param name="res"></param>
        /// <param name="control"></param>
        /// <param name="lang"></param>
        private static void ApplyResourceToControl(ComponentResourceManager res, Control control, CultureInfo lang)
        {
            if (control.GetType() == typeof(MenuStrip))  // See if this is a menuStrip
            {
                MenuStrip strip = (MenuStrip)control;

                ApplyResourceToToolStripItemCollection(strip.Items, res, lang);
            }

            foreach (Control c in control.Controls) // Apply to all sub-controls
            {
                ApplyResourceToControl(res, c, lang);
                res.ApplyResources(c, c.Name, lang);
            }

            // Apply to self
            res.ApplyResources(control, control.Name, lang);
        }

        private static void ApplyResourceToToolStripItemCollection(ToolStripItemCollection col, ComponentResourceManager res, CultureInfo lang)
        {
            for (int i = 0; i < col.Count; i++)     // Apply to all sub items
            {
                ToolStripItem item = col[i];

                var tsmItem = item as ToolStripMenuItem;
                if (tsmItem != null)
                    ApplyResourceToToolStripItemCollection(tsmItem.DropDownItems, res, lang);

                res.ApplyResources(item, item.Name, lang);
            }
        }

    }
}
