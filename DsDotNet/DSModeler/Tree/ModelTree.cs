namespace DSModeler.Tree;
[SupportedOSPlatform("windows")]
public static class ModelTree
{
    public static void ClearSubBtn(AccordionControlElement ace)
    {
        clearSubElements(ace);

        static void clearSubElements(AccordionControlElement ele)
        {
            foreach (AccordionControlElement subEle in ele.Elements)
            {
                clearSubElements(subEle);
            }

            ele.Elements.Clear();
        }
    }

    private static List<AccordionControlElement> appandFlows(FormMain formMain, Dictionary<Flow, ViewNode> viewAll, DsSystem sys, AccordionControlElement ele)
    {
        List<AccordionControlElement> lstAce = new();
        foreach (Flow flow in sys.Flows)
        {
            if (!viewAll.ContainsKey(flow))
            {
                continue;// flow에 내용이 없는것은 생략
            }

            AccordionControlElement eleFlow = new()
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
            AccordionControlElement ele = new()
            { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
            ele.Click += (s, e) => formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;

            formMain.Ace_System.Elements.Add(ele);

            List<AccordionControlElement> lstFlowAce = appandFlows(formMain, viewAll, sys, ele);
            lstFlowAce.ForEach(f =>
                f.Click += (s, e) =>
                {
                    ViewNode viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                    DocContr.CreateDocOrSelect(formMain, viewNode);
                });

            _ = sys.ExternalSystems.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, formMain.Ace_ExSystem));
            _ = sys.Devices.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, formMain.Ace_Device));
        });
    }
    private static void createLoadedSystemBtn(FormMain formMain, DsSystem sys
        , Dictionary<Flow, ViewNode> viewAll
        , AccordionControlElement eleParent)
    {
        formMain.Do(() =>
        {
            AccordionControlElement ele = new()
            { Style = ElementStyle.Group, Text = sys.Name, Tag = sys };
            ele.Click += (s, e) => formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;

            List<AccordionControlElement> lstFlowAce = appandFlows(formMain, viewAll, sys, ele);
            lstFlowAce.ForEach(f =>
                f.Click += (s, e) =>
                {
                    ViewNode viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                    DocContr.CreateDocOrSelect(formMain, viewNode);
                });

            eleParent.Elements.Add(ele);

            _ = sys.LoadedSystems.Iter(s => createLoadedSystemBtn(formMain, s.ReferenceSystem, viewAll, ele));
        });
    }

}



