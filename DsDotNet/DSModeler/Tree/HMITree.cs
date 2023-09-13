using static Engine.CodeGenCPU.TagManagerModule;

namespace DSModeler.Tree
{
    public static class HMITree
    {
        private static readonly Color startColor = Color.Green;
        private static readonly Color resetColor = Color.Red;
        private static readonly string startToolTip = "START";
        private static readonly string resetToolTip = "RESET";
        private static readonly Color offColor = Color.RoyalBlue;
        public static async Task CreateHMIBtn(FormMain formMain, DsSystem sys, IEnumerable<ViewNode> views)
        {
            await formMain.DoAsync(tsc =>
            {
                //var eleSys = new AccordionControlElement()
                //{ Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
                //eleSys.Click += (s, e) =>
                //{
                //    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                //};
                //formMain.Ace_HMI.Elements.Add(eleSys);
                Dictionary<Flow, ViewNode> nodeFlows = views.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                         .Where(w => w.UsedViewNodes.Any())
                                         .ToDictionary(s => s.Flow.Value, s => s);
                foreach (KeyValuePair<Flow, ViewNode> flowDic in nodeFlows)
                {
                    Flow flow = flowDic.Key;
                    AccordionControlElement eleFlow = new()
                    { Style = ElementStyle.Group, Text = flow.Name, Tag = flow };
                    eleFlow.Click += (s, e) =>
                    {
                        Flow eleTagFlow = ((AccordionControlElement)s).Tag as Flow;
                        formMain.PropertyGrid.SelectedObject = eleTagFlow;

                        DocContr.CreateDocOrSelect(formMain, flowDic.Value);
                    };
                    formMain.Ace_HMI.Elements.Add(eleFlow);

                    flow.Graph.Vertices
                   .OrderBy(v => v.QualifiedName)
                   .OfType<Real>()
                   .Iter(v =>
                   {
                       AccordionControlElement realEle = new() { Style = ElementStyle.Item, Text = $"{v.Name}", Tag = v };
                       realEle.Click += (s, e) =>
                       {
                           formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                       };
                       AccordionContextButton acb1 = createAcb(v, false);
                       AccordionContextButton acb2 = createAcb(v, true);

                       _ = realEle.ContextButtons.Add(acb1);
                       _ = realEle.ContextButtons.Add(acb2);
                       eleFlow.Elements.Add(realEle);
                   });
                }
                AccordionContextButton createAcb(Vertex v, bool start)
                {

                    AccordionContextButton acb = new()
                    {
                        Tag = v,
                        Visibility = DevExpress.Utils.ContextItemVisibility.Visible
                    };

                    if (start)
                    {
                        acb.Click += (s, e) =>
                        {
                            AccordionContextButton btn = UpdateBtn(s);
                            StartHMI(btn.Tag as Real);
                        };
                        acb.AppearanceHover.ForeColor = startColor;
                        acb.ToolTip = startToolTip;
                    }
                    else
                    {
                        acb.Click += (s, e) =>
                        {
                            AccordionContextButton btn = UpdateBtn(s);
                            ResetHMI(btn.Tag as Real);
                        };
                        acb.AppearanceHover.ForeColor = resetColor;
                        acb.ToolTip = resetToolTip;
                    }

                    acb.AppearanceHover.Options.UseForeColor = true;
                    acb.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.True;
                    acb.AlignmentOptions.Panel = DevExpress.Utils.ContextItemPanel.Center;
                    acb.AlignmentOptions.Position = DevExpress.Utils.ContextItemPosition.Far;
                    acb.Id = Guid.NewGuid();
                    acb.ImageOptionsCollection.ItemNormal.UseDefaultImage = true;
                    acb.Name = acb.Id.ToString();

                    return acb;

                    void StartHMI(Real real)
                    {
                        _ = Task.Run(() =>
                        {
                            VertexManager vv = real.TagManager as VertexManager;
                            vv.SF.Value = true;
                            //await Task.Delay(500);  //CPU에서 자동으로 NotifyPostExcute에서 꺼짐 Web HMI 붙히면 변경 필요
                            //vv.SF.Value = false;
                        });
                    }
                    void ResetHMI(Real real)
                    {
                        _ = Task.Run(() =>
                        {
                            VertexManager vv = real.TagManager as VertexManager;
                            vv.RF.Value = true;
                            //await Task.Delay(500);  //CPU에서 자동으로 NotifyPostExcute에서 꺼짐 Web HMI 붙히면 변경 필요
                            //vv.RF.Value = false;
                        });
                    }
                }
                tsc.SetResult(true);
            });
        }

        private static AccordionContextButton UpdateBtn(object s)
        {
            AccordionContextButton btn = (AccordionContextButton)s;
            //if (btn.AppearanceNormal.ForeColor != offColor)
            //    btn.AppearanceNormal.ForeColor = offColor;
            //else
            //    btn.AppearanceNormal.ForeColor = btn.ToolTip == startToolTip ? startColor : resetColor;

            return btn;
        }
        public static void OffHMIBtn(AccordionControlElement ace_HMI)
        {
            OffHMISubBtn(ace_HMI);
        }
        private static void OffHMISubBtn(AccordionControlElement ace)
        {
            offSubElements(ace);

            static void offSubElements(AccordionControlElement ele)
            {
                foreach (AccordionControlElement subEle in ele.Elements)
                {
                    offSubElements(subEle);
                }

                _ = ele.Elements.Iter(i => i.ContextButtons
                        .Iter(b => b.AppearanceNormal.ForeColor = offColor));
            }
        }
    }
}
