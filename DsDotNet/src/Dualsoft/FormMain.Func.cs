using DevExpress.XtraBars.Docking2010.Views;
using DSModeler.Form;
using System.Linq;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ViewModule;

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
    }
}