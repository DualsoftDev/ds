using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using DevExpress.XtraEditors.Controls;
using Dsu.PLCConverter.UI;
//using Images = Dsu.Common.Resources.Images;

namespace MelsecConverter
{
    public partial class UcPanelLog : UserControl
    {
        public ListBoxItemCollection Items => listBoxControlOutput.Items;
        public int SelectedIndex { get => listBoxControlOutput.SelectedIndex; set => listBoxControlOutput.SelectedIndex = value; }

        public UcPanelLog()
        {
            InitializeComponent();
        }

        private void UcPaneLog_Load(object sender, EventArgs args)
        {
            listBoxControlOutput.Dock = DockStyle.Fill;
            listBoxControlOutput.AllowHtmlDraw = DevExpress.Utils.DefaultBoolean.True;

            var items = listBoxControlOutput.ContextMenuStrip.Items;
            items.Add(new ToolStripMenuItem("Clear", Images.Clear, (o, a) =>
            {
                listBoxControlOutput.Items.Clear();
                listBoxControlOutput.SelectedIndex = 0;
            }));

            items.Add(new ToolStripMenuItem("Copy all", Images.Copy, (o, a) =>
            {
                var strings =
                    from n in Enumerable.Range(0, listBoxControlOutput.Items.Count)
                    let t = listBoxControlOutput.Items[n].ToString()
                    select Regex.Replace(t, "<.*?>", "")
                    ;

                var text = String.Join("\r\n", strings);
                Clipboard.SetText(text);
            }));

            items.Add(new ToolStripMenuItem("Copy selected", Images.Copy, (o, a) =>
            {
                var strings =
                    from item in listBoxControlOutput.SelectedItems
                    let str = item.ToString()
                    select Regex.Replace(str, "<.*?>", "")
                ;

                var text = String.Join("\r\n", strings);
                Clipboard.SetText(text);
            }));

            listBoxControlOutput.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right)
                    listBoxControlOutput.ContextMenuStrip.Show();
            };

        }
    }
}
