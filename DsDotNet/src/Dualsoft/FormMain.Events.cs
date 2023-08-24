using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DSModeler.Form;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Microsoft.FSharp.Core;
using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;

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
 

            tabbedView_Doc.QueryControl += (s, e) =>
            {
                if (e.Control == null)  //Devexpress MDI Control
                    e.Control = new System.Windows.Forms.Control();
            };
            tabbedView_Doc.DocumentSelected += (s, e) =>
            {
                var docForm = e.Document.Tag as FormDocView;
                if (docForm != null && docForm.UcView.MasterNode != null)
                    ViewDraw.DrawStatus(docForm.UcView.MasterNode, docForm);
            };

            gridLookUpEdit_Expr.EditValueChanged += (s, e) =>
            {
                var textForm = DocControl.CreateDocExprOrSelect(this, tabbedView_Doc);
                if (textForm == null) return;
                DSFile.UpdateExpr(textForm, gridLookUpEdit_Expr.EditValue as LogicStatement);
            };
            gridLookUpEdit_Expr.BeforePopup += (s, e) =>
            {
                gridLookUpEdit_Expr.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            };
            gridLookUpEdit_Log.BeforePopup += (s, e) =>
            {
                gridLookUpEdit_Log.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            };
            comboBoxEdit_RunMode.EditValueChanging += (s, e) =>
            {
                Global.CpuRunMode = ToRuntimePackage(e.NewValue.ToString());
                RuntimeDS.Package = Global.CpuRunMode;
                DSRegistry.SetValue(K.CpuRunMode, Global.CpuRunMode);
                if(e.OldValue != null)
                    ImportPowerPointWapper(Files.GetLast());
            };

            spinEdit_StartIn.Properties.EditValueChanging += (s, e) => UpdateRunStartInOut(e, true);
            spinEdit_StartOut.Properties.EditValueChanging += (s, e) => UpdateRunStartInOut(e, false);
            void UpdateRunStartInOut(ChangingEventArgs e, bool bIn)
            {
                var textValue = e.NewValue.ToString().Split('.')[0];
                e.Cancel = Convert.ToInt32(textValue) < 0;
                if (!e.Cancel)
                {
                    if (bIn)
                    {
                        Global.RunStartIn = Convert.ToInt32(textValue);
                        DSRegistry.SetValue(K.RunStartIn, Global.RunStartIn);
                    }
                    else
                    {
                        Global.RunStartOut = Convert.ToInt32(textValue);
                        DSRegistry.SetValue(K.RunStartOut, Global.RunStartOut);
                    }
                }
            }

            toggleSwitch_menuExpand.Toggled += (s, e) =>
            {
                Global.LayoutMenumExpand = toggleSwitch_menuExpand.IsOn;
                DSRegistry.SetValue(K.LayoutMenuExpand, Global.LayoutMenumExpand);

                if (Global.LayoutMenumExpand)
                {
                    ac_Main.RootDisplayMode = DevExpress.XtraBars.Navigation.AccordionControlRootDisplayMode.Default;
                    ac_Main.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.Standard;
                }
                else
                {

                    ac_Main.RootDisplayMode = DevExpress.XtraBars.Navigation.AccordionControlRootDisplayMode.Footer;
                    ac_Main.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
                }
            };

            toggleSwitch_LayoutGraph.Toggled += (s, e) =>
            {
                Global.LayoutGraphLineType = toggleSwitch_LayoutGraph.IsOn;
                DSRegistry.SetValue(K.LayoutGraphLineType, Global.LayoutGraphLineType);
            };
       
            toggleSwitch_showDeviceExpr.Toggled += (s, e) =>
            {
                LogicTree.UpdateExpr(gridLookUpEdit_Expr, toggleSwitch_showDeviceExpr.IsOn);
            };


            textEdit_IP.TextChanged += (s, e) =>
            {
                IPAddress.TryParse(textEdit_IP.Text, out IPAddress addr);
                if (addr == null) return;

                if (Global.CpuRunMode.IsPackagePC())
                {
                    DSRegistry.SetValue(K.RunHWIP, textEdit_IP.Text);
                    _PaixNMF?.Dispose();
                    _PaixNMF = new PaixDriver(textEdit_IP.Text, Global.RunStartIn, Global.RunStartOut);
                    var a = _PaixNMF.Open();
                }
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
                    var visibleFroms = tabbedView_Doc.Documents
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

            Global.StatusChangeLogCount.Subscribe(rx =>
            {
                this.Do(() =>
                {
                    barStaticItem_logCnt.Caption 
                        = $"logs:{rx.Item1} TimeSpan {rx.Item2:ss\\.fff}sec";
                });
            });

        }

      

    }
}