using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static partial class EmControl
    {
        /// <summary>
        /// http://stackoverflow.com/questions/301678/embedding-a-winform-within-a-winform-c
        /// </summary>
        /// <param name="me">embedding 시킬 form</param>
        /// <param name="targetContainer">form 이 embedding 될 container control</param>
        /// <param name="fill"></param>
        public static void EmbedTo(this Control me, Control targetContainer, bool fill = true)
        {
            //Form frm = me as Form;
            if (me is Form frm)
            {
                frm.TopLevel = false;
                frm.FormBorderStyle = FormBorderStyle.None;
                frm.Visible = true;
            }
            if (fill)
                me.Dock = DockStyle.Fill;
            targetContainer.Controls.Add(me);
        }

        public static Form EmbedForm(this Control me)
        {
            Form form = new Form() { Dock = DockStyle.Fill, Width=me.Width, Height=me.Height };
            int n = me.Controls.Count;
            for ( int i = n-1; i >=0 ; i--)
            {
                var c = me.Controls[i];
                form.Controls.Add(c);
                me.Controls.Remove(c);
            }
            form.Show();
            form.EmbedTo(me);
            return form;
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


        //public static void SetLanguage(this Form form, SupportedCultures lang)
        //{
        //    SetLanguage(form, lang.ConvertToString());
        //}
        //public static void SetLanguage(this Form form, string lang)
        //{
        //    var ci = new CultureInfo(lang);
        //    ci.Apply();

        //    ComponentResourceManager rm = new ComponentResourceManager(form.GetType());
        //    ApplyResourceToControl(rm, form, ci);
        //    //form.CollectChildren().ForEach(c => rm.ApplyResources(c, c.Name, ci));
        //    rm.ApplyResources(form, "$this", ci);
        //}

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

                if (item is ToolStripMenuItem)
                {
                    ToolStripMenuItem menuitem = (ToolStripMenuItem)item;
                    ApplyResourceToToolStripItemCollection(menuitem.DropDownItems, res, lang);
                }

                res.ApplyResources(item, item.Name, lang);
            }
        }


        // DockStyle.Top 순서 재지정.  array 의 순서대로 Top Docking
        public static void RearrangeDockTops(this Control[] controls)
        {
            foreach (var c in controls)
            {
                c.Dock = DockStyle.Top;
                c.BringToFront();
            }
        }
    }
}
