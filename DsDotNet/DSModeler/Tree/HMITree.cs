using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Import.Office;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Import.Office.ViewModule;

namespace DSModeler.Tree
{
    public static class HMITree
    {
        static readonly Color startColor = Color.Green;
        static readonly Color resetColor = Color.Red;
        static readonly string startToolTip = "START";
        static readonly string resetToolTip = "RESET";
        static readonly Color offColor = Color.RoyalBlue;
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
                var nodeFlows = views.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                         .Where(w => w.UsedViewNodes.Any())
                                         .ToDictionary(s => s.Flow.Value, s => s);
                foreach (var flowDic in nodeFlows)
                {
                    var flow = flowDic.Key;
                    var eleFlow = new AccordionControlElement()
                    { Style = ElementStyle.Group, Text = flow.Name, Tag = flow };
                    eleFlow.Click += (s, e) =>
                    {
                        var eleTagFlow = ((AccordionControlElement)s).Tag as Flow;
                        formMain.PropertyGrid.SelectedObject = eleTagFlow;

                        DocControl.CreateDocOrSelect(formMain, flowDic.Value);
                    };
                    formMain.Ace_HMI.Elements.Add(eleFlow);

                    flow.Graph.Vertices
                   .OrderBy(v => v.QualifiedName)
                   .OfType<Real>()
                   .ForEach(v =>
                   {
                       var realEle = new AccordionControlElement() { Style = ElementStyle.Item, Text = $"{v.Name}", Tag = v };
                       realEle.Click += (s, e) =>
                       {
                           formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                       };
                       AccordionContextButton acb1 = createAcb(v, false);
                       AccordionContextButton acb2 = createAcb(v, true);

                       realEle.ContextButtons.Add(acb1);
                       realEle.ContextButtons.Add(acb2);
                       eleFlow.Elements.Add(realEle);
                   });
                }
                AccordionContextButton createAcb(Vertex v, bool start)
                {

                    var acb = new AccordionContextButton() { Tag = v };
                    acb.Visibility = DevExpress.Utils.ContextItemVisibility.Visible;

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
                        Task.Run(() =>
                        {
                            var vv = (real.TagManager as VertexManager);
                            vv.SF.Value = true;
                            //await Task.Delay(500);  //CPU에서 자동으로 NotifyPostExcute에서 꺼짐 Web HMI 붙히면 변경 필요
                            //vv.SF.Value = false;
                        });
                    }
                    void ResetHMI(Real real)
                    {
                        Task.Run(() =>
                        {
                            var vv = (real.TagManager as VertexManager);
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
            var btn = (AccordionContextButton)s;
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

            void offSubElements(AccordionControlElement ele)
            {
                foreach (var subEle in ele.Elements)
                    offSubElements(subEle);
                ele.Elements.Iter(i => i.ContextButtons
                        .Iter(b => b.AppearanceNormal.ForeColor = offColor));
            }
        }
    }
}
