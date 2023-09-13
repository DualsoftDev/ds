
using DataFormats = System.Windows.Forms.DataFormats;
using DragDropEffects = System.Windows.Forms.DragDropEffects;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        private void InitializationEventSetting()
        {
            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            };
            DragDrop += async (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    await ImportPowerPointWapper(files);
                }
            };
            KeyDown += async (s, e) =>
            {
                if (e.KeyData == Keys.F4)
                {
                    await ImportPowerPointWapper(null);
                }

                if (e.KeyData == Keys.F5)
                {
                    await ImportPowerPointWapper(Files.GetLast());
                }
            };


            TabbedView.QueryControl += (s, e) =>
            {
                e.Control ??= new System.Windows.Forms.Control();
            };


            gle_Expr.EditValueChanged += (s, e) =>
            {

                var textForm = DocContr.CreateDocExprOrSelect(this, TabbedView);
                if (textForm == null)
                {
                    return;
                }

                DSFile.UpdateExpr(textForm, gle_Expr.EditValue as LogicStatement);
            };

            gle_HW.EditValueChanged += (s, e) =>
            {
                //gleView_HW.GridControl.ExportToXlsx(@"D:\hwMaker.xlsx");
                HwModel hw = HwModels.GetModelByNumber((int)gle_HW.EditValue);
                if (hw != null)
                {
                    Global.DSHW = hw;
                    DSRegistry.SetValue(RegKey.RunHWDevice, hw.ToTextRegister);
                }
            };
            

            gle_Expr.BeforePopup += (s, e) =>
                gle_Expr.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            gle_HW.BeforePopup += (s, e) =>
                gle_HW.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            gle_Log.BeforePopup += (s, e) =>
                gle_Log.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            gle_Device.BeforePopup += (s, e) =>
                gle_Device.Properties.BestFitMode = BestFitMode.BestFitResizePopup;

            comboBoxEdit_RunMode.EditValueChanging += async (s, e) =>
            {
                Global.CpuRunMode = ToRuntimePackage(e.NewValue.ToString());
                RuntimeDS.Package = Global.CpuRunMode;
                DSRegistry.SetValue(RegKey.CpuRunMode, Global.CpuRunMode);
                if (e.OldValue != null)
                {
                    await ImportPowerPointWapper(Files.GetLast());
                }
            };

            spinEdit_StartIn.Properties.EditValueChanged += (s, e) =>
            {
                Global.RunCountIn = Convert.ToInt32(spinEdit_StartIn.EditValue);
                DSRegistry.SetValue(RegKey.RunCountIn, Global.RunCountIn);
            };
            spinEdit_StartOut.Properties.EditValueChanged += (s, e) =>
            {
                Global.RunCountOut = Convert.ToInt32(spinEdit_StartOut.EditValue);
                DSRegistry.SetValue(RegKey.RunCountOut, Global.RunCountOut);
            };

            toggleSwitch_menuExpand.Toggled += (s, e) =>
            {
                Global.LayoutMenumExpand = toggleSwitch_menuExpand.IsOn;
                DSRegistry.SetValue(RegKey.LayoutMenuExpand, Global.LayoutMenumExpand);

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
                DSRegistry.SetValue(RegKey.LayoutGraphLineType, Global.LayoutGraphLineType);
            };

            toggleSwitch_showDeviceExpr.Toggled += (s, e) =>
            {
                LogicTree.UpdateExpr(gle_Expr, toggleSwitch_showDeviceExpr.IsOn);
            };


            textEdit_IP.EditValueChanging += (s, e) =>
            {
                _ = IPAddress.TryParse(e.NewValue.ToString(), out IPAddress addr);
                if (addr == null)
                {
                    return;
                }

                DSRegistry.SetValue(RegKey.RunHWIP, e.NewValue);
                Global.RunHWIP = e.NewValue.ToString();

                if (Global.CpuRunMode.IsPackagePC() && PcContr.RunCpus.Any())
                {
                    PcAction.CreateConnect();
                }
            };

    
            _ = Global.ChangeLogCount.Subscribe(rx =>
            {
                this.Do(() =>
                {
                    LogCountText.Caption
                        = $"logs:{rx.Item1} TimeSpan {rx.Item2:ss\\.fff}sec";
                });
            });

            _ = DsProcessEvent.ProcessSubject.Subscribe(rx =>
            {
                this.Do(() =>
                {
                    barEditItem_Process.EditValue = rx.pro;
                    barStaticItem_procText.Caption = $"{rx.pro}%";
                });
            });

            _ = ViewDraw.StatusChangeSubject.Subscribe(rx =>
            {
                switch (rx.TagKind)
                {
                    case VertexTag.ready: ViewDraw.DicSV[rx.Target]= Tuple.Create(Status4.Ready, ViewDraw.DicSV[rx.Target].Item2);   break;
                    case VertexTag.going: ViewDraw.DicSV[rx.Target]= Tuple.Create(Status4.Going, ViewDraw.DicSV[rx.Target].Item2);   break;
                    case VertexTag.finish:ViewDraw.DicSV[rx.Target] = Tuple.Create(Status4.Finish, ViewDraw.DicSV[rx.Target].Item2); break;
                    case VertexTag.homing: ViewDraw.DicSV[rx.Target] = Tuple.Create(Status4.Homing, ViewDraw.DicSV[rx.Target].Item2); break;
                    default: break;
                }

                List<Tuple<FormDocView, ViewNode>> ret = GetViewNode(rx.Target);
                foreach (Tuple<FormDocView, ViewNode> r in ret)
                {
                    FormDocView form = r.Item1;
                    ViewNode node = r.Item2;
                    node.Status4 = ViewDraw.DicSV[rx.Target].Item1;
                    form.UcView.UpdateStatus(node);
                }
            });

            _ = ViewDraw.ActionChangeSubject.Subscribe(rx =>
            {
                var vertex = rx.Item1;
                var value = Convert.ToBoolean(rx.Item2);

                ViewDraw.DicSV[vertex] = Tuple.Create(ViewDraw.DicSV[vertex].Item1, value);

                List<Tuple<FormDocView, ViewNode>> ret = GetViewNode(vertex);
                foreach (Tuple<FormDocView, ViewNode> r in ret)
                {
                    FormDocView form = r.Item1;
                    ViewNode node = r.Item2;
                    form.UcView.UpdateValue(node, rx.Item2);
                }
            });

            List<Tuple<FormDocView, ViewNode>> GetViewNode(Vertex v)
            {
                return TabbedView.Documents
                             .Where(d => d.IsVisible)
                             .Select(d => d.Tag)
                             .OfType<FormDocView>()
                             .Where(w => w.UcView.Flow == v.Parent.GetFlow())
                             .Select(s => Tuple.Create(s, s.UcView.MasterNode.UsedViewVertexNodes(false)[v]))
                             .ToList();

            }
        }
    }
}