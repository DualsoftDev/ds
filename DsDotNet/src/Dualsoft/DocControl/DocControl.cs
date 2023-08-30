using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraEditors;
using DSModeler.Form;
using System.IO;
using System.Linq;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class DocControl
    {
        private static XtraForm CreateDocForm(
            XtraForm formChiild
          , FormMain formParent, TabbedView tab, string docKey)
        {
            var document = GetDoc(tab, docKey);
            if (document != null)
            {
                tab.Controller.Activate(document);
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
            return tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
        }

        public static void CreateDocStart(FormMain formParent, TabbedView tab)
        {
            string docKey = K.DocStartPage;
            CreateDocForm(new FormDocImage(), formParent, tab, docKey);
        }

        public static void CreateDocDS(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT()) return;
            string docKey = K.DocDS;

            FormDocText formChiild = new FormDocText();
            CreateDocForm(formChiild, formParent, tab, docKey);

            DSFile.DrawDSText(formChiild);
        }

        public static FormDocText CreateDocExprOrSelect(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT()) return null;
            string docKey = K.DocExpression;

            FormDocText formChiild = new FormDocText();
            formChiild = CreateDocForm(formChiild, formParent, tab, docKey) as FormDocText;
            formChiild.Activate();
            return formChiild;

        }

        public static void CreateDocPLCLS(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT()) return;

            if (!RuntimeDS.Package.IsPackagePLC())
            {
                MBox.Warn("설정 H/W 에서 PLC를 선택해야 합니다.");
                return;
            }


            var fullpath = PLC.Export();
            string docKey = K.DocPLC;

            //Storages 연결이슈로  새로 준비 
            formParent.ImportPowerPointWapper(Files.GetLast());

            FormDocText formChiild = new FormDocText();
            CreateDocForm(formChiild, formParent, tab, docKey);
            formChiild.TextEdit.Text = File.ReadAllText(fullpath);
        }


        public static void CreateDocOrSelect(FormMain formParent, ViewNode v)
        {
            if (!Global.IsLoadedPPT()) return;
            Flow flow = v.Flow.Value;
            string docKey = flow.QualifiedName;

            FormDocView formChiild = new FormDocView();
            formChiild = CreateDocForm(formChiild, formParent, formParent.TabbedView, docKey) as FormDocView;
            if (formChiild.UcView.MasterNode == null)
                formChiild.UcView.SetGraph(v, flow, Global.LayoutGraphLineType);
            //상태 업데이트
            ViewDraw.DrawStatus(v, formChiild);
        }
    }
}


