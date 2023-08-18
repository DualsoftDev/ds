using DevExpress.XtraEditors;
using DSModeler.Form;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using System;
using System.Linq;
using System.Windows.Forms;

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
                    ImportPowerPointWapper(Files.GetLast());
            };

            tabbedView1.QueryControl += (s, e) =>
            {
                if (e.Control == null)  //Devexpress MDI Control
                    e.Control = new System.Windows.Forms.Control();
            };
            tabbedView1.DocumentSelected += (s, e) =>
            {
                var docForm = e.Document.Tag as FormDocView;
                if (docForm != null && docForm.UcView.MasterNode != null)
                    ViewDraw.DrawStatus(docForm.UcView.MasterNode, docForm);
            };

            gridLookUpEdit_Expr.EditValueChanged += (ss, ee) =>
            {
                var textForm = DocControl.CreateDocExpr(this, tabbedView1);
                if (textForm == null) return;
                DSFile.UpdateExpr(textForm, gridLookUpEdit_Expr.EditValue as LogicStatement);
            };
            gridLookUpEdit_Expr.BeforePopup += (ss, ee) =>
            {
                gridLookUpEdit_Expr.Properties.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            };
            gridLookUpEdit_Log.BeforePopup += (ss, ee) =>
            {
                gridLookUpEdit_Log.Properties.BestFitMode = DevExpress.XtraEditors.Controls.BestFitMode.BestFitResizePopup;
            };

            toggleSwitch_menuNonFooter.Toggled += (ss, ee) =>
            {
                Global.LayoutMenuFooter = toggleSwitch_menuNonFooter.IsOn;
                DSRegistry.SetValue(K.LayoutMenuFooter, Global.LayoutMenuFooter);

                if (toggleSwitch_menuNonFooter.IsOn)
                    ac_Main.RootDisplayMode = DevExpress.XtraBars.Navigation.AccordionControlRootDisplayMode.Default;
                else
                    ac_Main.RootDisplayMode = DevExpress.XtraBars.Navigation.AccordionControlRootDisplayMode.Footer;
            };


            DsProcessEvent.ProcessSubject.Subscribe(rx =>
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

            Global.StatusChangeLogCount.Subscribe(_ =>
            {
                this.Do(() =>
                {
                    barStaticItem_logCnt.Caption = $"logs:{LogicLog.ValueLogs.Count}";
                });
            });

        }


    }
}