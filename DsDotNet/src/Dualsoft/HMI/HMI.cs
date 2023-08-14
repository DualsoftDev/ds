using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class HMI
    {
        public static void CreateHMIBtn(FormMain formMain, AccordionControlElement ace_HMI, DsSystem sys)
        {
            var eleSys = new AccordionControlElement()
            { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
            eleSys.Click += (s, e) => {
                formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
            };
            ace_HMI.Elements.Add(eleSys);

            foreach (var flow in sys.Flows)
            {
                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = flow.Name, Tag = flow };
                eleFlow.Click += (s, e) => {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };
                eleSys.Elements.Add(eleFlow);

                flow.Graph.Vertices
               .OrderBy(v => v.QualifiedName)
               .OfType<Real>()
               .ForEach(v =>
               {
                   var realEle = new AccordionControlElement() { Style = ElementStyle.Item, Text = $"{v.Name}", Tag = v };
                   realEle.Click += (s, e) => { 
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

                if (start)
                {
                    acb.Click += (s, e) =>
                    {
                        AccordionContextButton btn = UpdateBtn(s);
                        StartHMI(btn.Tag as Real);
                    };
                    acb.AppearanceNormal.ForeColor = Color.Lime;
                    acb.AppearanceHover.ForeColor = Color.Green;
                    acb.ToolTip = "START";
                }
                else
                {
                    acb.Click += (s, e) => {
                        AccordionContextButton btn = UpdateBtn(s);
                        ResetHMI(btn.Tag as Real);
                    }; 
                    acb.AppearanceNormal.ForeColor = Color.IndianRed;
                    acb.AppearanceHover.ForeColor = Color.Red;
                    acb.ToolTip = "RESET";
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

                void StartHMI(Real real)
                {
                    var vv = real.TagManager as VertexManager;
                    vv.SF.Value = true;
                    vv.RF.Value = false;
                }
                void ResetHMI(Real real)
                {
                    var vv = real.TagManager as VertexManager;
                    vv.RF.Value = true;
                    vv.SF.Value = false;
                }
            }
        }

        private static AccordionContextButton UpdateBtn(object s)
        {
            var btn = (AccordionContextButton)s;
            var btns = btn.Collection.OfType<AccordionContextButton>().ToList();
            //전체 자동 숨기기
            btns.Iter(b => b.Visibility = DevExpress.Utils.ContextItemVisibility.Auto);
            //해당 버튼 만 보이기
            btn.Visibility = DevExpress.Utils.ContextItemVisibility.Visible;

            return btn;
        }
    }

}


