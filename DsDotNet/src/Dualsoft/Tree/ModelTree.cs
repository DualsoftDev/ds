using DevExpress.XtraBars.Navigation;
using Model.Import.Office;
using System.Collections.Generic;
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
            foreach (var v in ppt.Views)
            {
                if (v.ViewType != InterfaceClass.ViewType.VFLOW) continue;

                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Item, Text = v.Flow.Value.Name, Tag = v };
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
        }
    }
}


