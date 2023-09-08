using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using Dual.Common.Winform;
using System.Collections.Generic;
using static Engine.Core.CoreModule;
using static Engine.Import.Office.ViewModule;

namespace DSModeler.Tree;
[SupportedOSPlatform("windows")]
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

        private static List<AccordionControlElement> appandFlows(FormMain formMain, Dictionary<Flow, ViewNode> viewAll, DsSystem sys, AccordionControlElement ele)
        {
            List<AccordionControlElement> lstAce = new List<AccordionControlElement>();
            foreach (var flow in sys.Flows)
            {
                if (!viewAll.ContainsKey(flow)) continue;// flow에 내용이 없는것은 생략

                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Item, Text = flow.Name, Tag = viewAll[flow] };
                eleFlow.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };
                lstAce.Add(eleFlow);
                ele.Elements.Add(eleFlow);
            }
            return lstAce;
        }

        public static void CreateActiveSystemBtn(FormMain formMain, DsSystem sys, Dictionary<Flow, ViewNode> viewAll)
        {
            formMain.Do(() =>
            {
                var ele = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
                ele.Click += (s, e) => formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;

                formMain.Ace_System.Elements.Add(ele);

                var lstFlowAce = appandFlows(formMain, viewAll, sys, ele);
                lstFlowAce.ForEach(f =>
                    f.Click += (s, e) =>
                    {
                        var viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                        DocControl.CreateDocOrSelect(formMain, viewNode);
                    });

                sys.ExternalSystems.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, formMain.Ace_ExSystem));
                sys.Devices.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, formMain.Ace_Device));
            });
        }
        private static void createLoadedSystemBtn(FormMain formMain, DsSystem sys
            , Dictionary<Flow, ViewNode> viewAll
            , AccordionControlElement eleParent)
        {
            formMain.Do(() =>
            {
                var ele = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
                ele.Click += (s, e) => formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;

                var lstFlowAce = appandFlows(formMain, viewAll, sys, ele);
                lstFlowAce.ForEach(f =>
                    f.Click += (s, e) =>
                    {
                        var viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                        DocControl.CreateDocOrSelect(formMain, viewNode);
                    });

                eleParent.Elements.Add(ele);

                sys.LoadedSystems.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, ele));
            });
        }

    }
}


