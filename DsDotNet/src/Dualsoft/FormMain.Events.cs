using DevExpress.XtraEditors;
using DSModeler.Form;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using Dual.Common.Winform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {

        void InitializationEventSetting()
        {
            this.AllowDrop = true;
            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    ImportPowerPointWapper(files);
            };
            this.KeyDown += (s, e) =>
            {
                if (e.KeyData == Keys.F4)
                    ImportPowerPointWapper(null);
                if (e.KeyData == Keys.F5)
                    ImportPowerPointWapper(LastFiles.Get());
            };

            tabbedView1.QueryControl += (s, e) =>
            {
                if (e.Control == null)  //Devexpress MDI Control
                    e.Control = new System.Windows.Forms.Control();
            };
            tabbedView1.DocumentSelected += (s, e) =>
            {
                var docForm = e.Document.Tag as FormDocView;
                if (docForm != null)
                    DocControl.DrawStatus(this, docForm.UcView.MasterNode, docForm);

            };
            ProcessEvent.ProcessSubject.Subscribe(rx =>
            {
                this.Do(() =>
                {
                    barEditItem_Process.EditValue = rx.pro;
                    barStaticItem_procText.Caption = $"{rx.pro}%";
                });
            });

            Global.StatusChangeSubject.Subscribe(rx =>
            {
                this.Do(() =>
                {
                    var visibleFroms = tabbedView1.Documents
                                        .Where(w => w.IsVisible)
                                        .Select(s => s.Tag)
                                        .OfType<FormDocView>();

                    foreach (var form in visibleFroms)
                    {
                        var nodes = form.UcView.MasterNode
                                            .UsedViewNodes
                                            .Where(w => w.CoreVertex != null)
                                            .Where(f => f.CoreVertex.Value == rx.Item1);

                        if (nodes.Any())
                        {
                            var node = nodes.First();
                            node.Status4 = rx.Item2;
                            form.UcView.UpdateStatus(node);
                        }

                    }
                });
            });
        }
    }
}