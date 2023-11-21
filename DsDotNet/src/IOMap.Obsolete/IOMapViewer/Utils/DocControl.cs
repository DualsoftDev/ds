using IOMapViewer.Utils;

namespace IOMapViewer.DocControl;

[SupportedOSPlatform("windows")]
public static class DocContr
{
    private static XtraForm CreateDocForm(
        XtraForm formChiild
        , FormMain formParent, TabbedView tab, string docKey)
    {
        BaseDocument document = GetDoc(tab, docKey);
        if (document != null)
        {
            _ = tab.Controller.Activate(document);
            formChiild = document.Tag as XtraForm;
        }
        else
        {
            formChiild.Name = docKey;
            formChiild.MdiParent = formParent;
            formChiild.Text = docKey;
            document = GetDoc(tab, docKey);
            document.Caption = docKey;
            document.Tag = formChiild;

            formChiild.Show();
        }

        return formChiild;
    }

    private static BaseDocument GetDoc(TabbedView tab, string docKey)
    {
        return tab.Documents.FirstOrDefault(w => w.Control.Name == docKey);
    }


    public static void CreateDocDS(FormMain formParent, TabbedView tab)
    {
        FormView fv = new();
        CreateDocForm(fv, formParent, tab, "MAP");
    }
}