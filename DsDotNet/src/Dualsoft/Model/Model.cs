using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class Model
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

        public static List<AccordionControlElement> AppandFlows(PptResult ppt, AccordionControlElement ele)
        {
            List<AccordionControlElement> lstAce = new List<AccordionControlElement>();
            foreach (var v in ppt.Views)
            {
                if (v.ViewType != InterfaceClass.ViewType.VFLOW) continue;

                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Item, Text = v.Flow.Value.Name, Tag = v };
                lstAce.Add(eleFlow);
                ele.Elements.Add(eleFlow);
            }
            return lstAce;
        }
    }
}


