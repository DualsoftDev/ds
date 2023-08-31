using DevExpress.XtraBars.Navigation;
using Dual.Common.Winform;
using Model.Import.Office;
using System.Collections.Generic;
using System.Linq;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

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

        public static List<AccordionControlElement> AppandFlows(FormMain formMain, PptResult ppt, AccordionControlElement ele)
        {
            List<AccordionControlElement> lstAce = new List<AccordionControlElement>();
            var nodeFlows = ppt.Views.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
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

        public static void CreateModelBtn(FormMain formMain, PptResult ppt)
        {
            formMain.Do(() =>
            {
                var ele = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = ppt.System.Name, Tag = ppt.System };
                ele.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };

                if (ppt.IsActive)
                    formMain.Ace_System.Elements.Add(ele);
                else
                    formMain.Ace_Device.Elements.Add(ele);

                var lstFlowAce = Tree.ModelTree.AppandFlows(formMain, ppt, ele);
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


