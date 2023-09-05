using DevExpress.XtraBars.Navigation;
using Dual.Common.Winform;
using Engine.Import.Office;
using System.Collections.Generic;
using System.Linq;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Import.Office.ImportPPTModule;
using static Engine.Import.Office.ViewModule;

namespace DSModeler.Tree
{
    public static class ModelTree
    {
        public static void ClearSubBtn(AccordionControlElement ace)
        {
            clearSubElements(ace);

            void clearSubElements(AccordionControlElement ele)
            {
                foreach (var subEle in ele.Elements)
                    clearSubElements(subEle);
                ele.Elements.Clear();
            }
        }

        public static List<AccordionControlElement> AppandFlows(FormMain formMain, IEnumerable<ViewNode> views, AccordionControlElement ele)
        {
            List<AccordionControlElement> lstAce = new List<AccordionControlElement>();
            var nodeFlows = views.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                     .Where(w => w.UsedViewNodes.Any())
                                     .ToDictionary(s => s.Flow.Value, s => s);
            foreach (var flowDic in nodeFlows)
            {
                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Item, Text = flowDic.Key.Name, Tag = flowDic.Value };
                eleFlow.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };
                lstAce.Add(eleFlow);
                ele.Elements.Add(eleFlow);
            }
            return lstAce;
        }

        public static void CreateModelBtn(FormMain formMain, DsSystem sys, IEnumerable<ViewNode> views, PouGen pou)
        {
            formMain.Do(() =>
            {
                var ele = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
                ele.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };

                if (pou.IsActive)
                    formMain.Ace_System.Elements.Add(ele);
                else if (pou.IsDevice)
                    formMain.Ace_Device.Elements.Add(ele);
                else if (pou.IsExternal)
                {
                    var extSys = pou.ToExternalSystem().Value;
                    ele.Text = extSys.Name; 
                    formMain.Ace_ExSystem.Elements.Add(ele);
                }

                var lstFlowAce = Tree.ModelTree.AppandFlows(formMain, views, ele);
                lstFlowAce.ForEach(f =>
                    f.Click += (s, e) =>
                    {
                        var viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                        DocControl.CreateDocOrSelect(formMain, viewNode);
                    });
            });
        }
    }
}


