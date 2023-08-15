using DevExpress.XtraBars.Navigation;
using Model.Import.Office;
using System.Collections.Generic;
using static Model.Import.Office.ImportPPTModule;

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


    }
}


