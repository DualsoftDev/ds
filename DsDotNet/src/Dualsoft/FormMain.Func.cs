using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DSModeler.Form;
using Dual.Common.Core;
using System;
using System.Linq;

using Engine.Core;

using static Engine.CodeGenCPU.ConvertCoreExt;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagModule;
using static Model.Import.Office.ViewModule;
using DevExpress.XtraBars.Navigation;
using System.Drawing;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {

        private void CreateDocOrSelect(ViewNode v)
        {
            Flow flow = v.Flow.Value;
            string docKey = flow.QualifiedName;
            BaseDocument document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            if (document != null) tabbedView1.Controller.Activate(document);
            else
            {
                var view = new FormDocView();
                view.Name = docKey;
                view.MdiParent = this;
                view.Text = docKey;
                view.UcView.SetGraph(v, flow);
                view.Show();
                document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
            }
        }

        private void CreateDocStart()
        {
            string docKey = "Start";

            var view = new FormDocViewStart();
            view.Name = docKey;
            view.MdiParent = this;
            view.Text = docKey;
            view.Show();

            var document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            document.Caption = docKey;
        }



        private void RunSimMode(DsSystem sys)
        {
            var sysBits = Enum.GetValues(typeof(SystemTag)).Cast<SystemTag>();
            sysBits
                .Select(f => TagInfoType.GetTagSys(sys, f))
                .OfType<PlanVar<bool>>()
                .ForEach(tag =>
                {
                    int kind = ((IStorage)tag).TagKind;
                    if (
                       kind == (int)SystemTag.auto
                        || kind == (int)SystemTag.drive
                        || kind == (int)SystemTag.ready
                        || kind == (int)SystemTag.sim
                        )
                        tag.Value = true;
                });
        }

        private void CreateHMI(DsSystem sys)
        {
            var eleSys = new AccordionControlElement()
            { Style = ElementStyle.Group, Text = sys.Name };
            ace_HMI.Elements.Add(eleSys);

            foreach (var flow in sys.Flows)
            {
                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = flow.Name };
                eleSys.Elements.Add(eleFlow);

                flow.Graph.Vertices
               .OrderBy(v => v.QualifiedName)
               .OfType<Real>()
               .ForEach(v =>
               {
                   var realEle = new AccordionControlElement() { Style = ElementStyle.Item, Text = $"{v.Name}" };
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
                    acb.Click += (s, e) => { StartHMI(((AccordionContextButton)s).Tag as Real); };
                    acb.AppearanceHover.ForeColor = Color.Lime;
                    acb.AppearanceHover.BackColor = Color.Lime;
                    acb.HyperLinkColor = Color.Lime;
                    acb.ToolTip = "START";
                }
                else
                {
                    acb.Click += (s, e) => { ResetHMI(((AccordionContextButton)s).Tag as Real); };
                    acb.HyperLinkColor = Color.Magenta;
                    acb.ToolTip = "RESET";
                }
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



     
    }
}