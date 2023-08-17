using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using Dual.Common.Winform;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;

namespace DSModeler.Tree
{
    public static class HMITree
    {
        static readonly Color startColor = Color.Green;
        static readonly Color resetColor = Color.Red;
        static readonly string startToolTip = "START";
        static readonly string resetToolTip = "RESET";
        static readonly Color offColor = Color.RoyalBlue;
        public static async Task CreateHMIBtn(FormMain formMain, AccordionControlElement ace_HMI, DsSystem sys)
        {
            await formMain.DoAsync(tsc =>
            {
                var eleSys = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
                eleSys.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };
                ace_HMI.Elements.Add(eleSys);
                foreach (var flow in sys.Flows)
                {
                    var eleFlow = new AccordionControlElement()
                    { Style = ElementStyle.Group, Text = flow.Name, Tag = flow };
                    eleFlow.Click += (s, e) =>
                    {
                        formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                    };
                    eleSys.Elements.Add(eleFlow);

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
                            bool on = btn.AppearanceNormal.ForeColor != offColor;
                            StartHMI(btn.Tag as Real, on);
                        };
                        acb.AppearanceNormal.ForeColor = Color.RoyalBlue;
                        acb.AppearanceHover.ForeColor = startColor;
                        acb.ToolTip = startToolTip;
                    }
                    else
                    {
                        acb.Click += (s, e) =>
                        {
                            AccordionContextButton btn = UpdateBtn(s);
                            bool on = btn.AppearanceNormal.ForeColor != offColor;
                            ResetHMI(btn.Tag as Real, on);
                        };
                        acb.AppearanceNormal.ForeColor = Color.RoyalBlue;
                        acb.AppearanceHover.ForeColor = resetColor;
                        acb.ToolTip = resetToolTip;
                    }

                    acb.AppearanceNormal.Options.UseForeColor = true;
                    acb.AppearanceHover.Options.UseForeColor = true;
                    acb.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.True;
                    acb.AlignmentOptions.Panel = DevExpress.Utils.ContextItemPanel.Center;
                    acb.AlignmentOptions.Position = DevExpress.Utils.ContextItemPosition.Far;
                    acb.Id = Guid.NewGuid();
                    acb.ImageOptionsCollection.ItemNormal.UseDefaultImage = true;
                    acb.Name = acb.Id.ToString();

                    return acb;

                    void StartHMI(Real real, bool on)
                    {
                        Task.Run(() =>
                        {
                            var vv = (real.TagManager as VertexManager);
                            vv.SF.Value = on;
                        });
                    }
                    void ResetHMI(Real real, bool on)
                    {
                        Task.Run(() =>
                        {
                            var vv = (real.TagManager as VertexManager);
                            vv.RF.Value = on;
                        });
                    }
                }
                tsc.SetResult(true);
            });
        }


        private static AccordionContextButton UpdateBtn(object s)
        {
            var btn = (AccordionContextButton)s;
            if (btn.AppearanceNormal.ForeColor != offColor)
                btn.AppearanceNormal.ForeColor = offColor;
            else
                btn.AppearanceNormal.ForeColor = btn.ToolTip == startToolTip ? startColor : resetColor;

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


