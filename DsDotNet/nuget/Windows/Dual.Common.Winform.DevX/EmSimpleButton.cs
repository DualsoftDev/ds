using DevExpress.XtraEditors;

using System;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static class EmSimpleButton
    {
        /// modeless dialog 이지만, modal 인 것 처럼 하나만 띄울 수 있도록..
        public static Form BindSingletonForm(this SimpleButton button, Func<Form> formCreator)
        {
            Form form = null;
            button.Click += (s, e) =>
            {
                if (form == null || form.IsDisposed)
                {
                    form = formCreator();
                    if (form == null)
                        return;

                    form.PlaceAtScreenCenter();
                    form.FormClosed += (s, e) => form = null;
                    form.Show();
                }
                else
                    form.PlaceAtScreenCenter().Activate(); //Focus();
            };
            return form;
        }
    }
}
