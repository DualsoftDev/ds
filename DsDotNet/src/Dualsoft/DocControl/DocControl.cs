using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Navigation;
using DSModeler.Form;
using Dual.Common.Core;
using Model.Import.Office;
using System;
using System.Drawing;
using System.Linq;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;
using DevExpress.XtraEditors;

namespace DSModeler
{
    public static class DocControl
    {
        public static void CreateDocStart(XtraForm form, TabbedView tab)
        {
            string docKey = "Start";

            var view = new FormDocViewStart();
            view.Name = docKey;
            view.MdiParent = form;
            view.Text = docKey;
            view.Show();

            var document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            document.Caption = docKey;
        }

       
        public static void CreateDocOrSelect(XtraForm form, TabbedView tab, ViewNode v)
        {
            Flow flow = v.Flow.Value;
            string docKey = flow.QualifiedName;
            BaseDocument document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            if (document != null) tab.Controller.Activate(document);
            else
            {
                var view = new FormDocView();
                view.Name = docKey;
                view.MdiParent = form;
                view.Text = docKey;
                view.UcView.SetGraph(v, flow);
                view.Show();
                document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
            }
        }
    }
}


