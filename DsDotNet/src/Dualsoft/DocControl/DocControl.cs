using DevExpress.Utils.Extensions;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraSplashScreen;
using DSModeler.Form;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Engine.CodeGenCPU.ExportModule;
using static Engine.Core.CoreModule;
using static Engine.Core.SystemToDsExt;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class DocControl
    {
     
        public static void CreateDocStart(FormMain form, TabbedView tab)
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
        public static void CreateDocDS(FormMain form, TabbedView tab)
        {
            if (!form.IsLoadedPPT()) return;
            string docKey = "ControlPC";
            string dsText = "";
            form.DicCpu.Keys.ForEach(sys =>
            {
                dsText += $"{sys.ToDsText()}\r\n\r\n";
            });

            BaseDocument document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            FormDocDS view;
            if (document != null)
            {
                tab.Controller.Activate(document);
                view = document.Tag as FormDocDS;
            }
            else
            {
                view = new FormDocDS();
                view.Name = docKey;
                view.MdiParent = form;
                view.Text = docKey;
                view.MemoEditDS.Text = dsText;
                document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
                document.Tag = view;

                view.Show();

            }


        }

        public static void CreateDocPLCLS(FormMain form, TabbedView tab)
        {
            if (!form.IsLoadedPPT()) return;

            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            string docKey = "PLC_LS";

            var xmlTemplateFile = Path.ChangeExtension(LastFiles.Get().First(), "xml");
            var xmlFileName = Path.GetFileNameWithoutExtension(xmlTemplateFile) + "_gen.xml";
            var xmlDriectory = Path.GetDirectoryName(xmlTemplateFile);
            var fullpath = Path.Combine(xmlDriectory, xmlFileName);

            if (File.Exists(xmlTemplateFile))
                //사용자 xg5000 Template 형식으로 생성
                ExportModuleExt.ExportXMLforXGI(form.ActiveSys, fullpath, xmlTemplateFile);
            else  //기본 템플릿 CPU-E 타입으로 생성
                ExportModuleExt.ExportXMLforXGI(form.ActiveSys, fullpath, null);


            BaseDocument document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            FormDocPLCLS view;
            if (document != null)
            {
                tab.Controller.Activate(document);
                view = document.Tag as FormDocPLCLS;
            }
            else
            {
                view = new FormDocPLCLS();
                view.Name = docKey;
                view.MdiParent = form;
                view.Text = docKey;
                document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
                document.Tag = view;

                view.Show();
            }

            view.MemoEditDS.Text = File.ReadAllText(fullpath);

            //Storages 연결이슈로  새로 준비 
            form.ImportPowerPointWapper(LastFiles.Get());

            SplashScreenManager.CloseForm();
        }

        public static void CreateDocOrSelect(FormMain form, TabbedView tab, ViewNode v)
        {
            Flow flow = v.Flow.Value;
            string docKey = flow.QualifiedName;
            BaseDocument document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            FormDocView view;
            if (document != null)
            {
                tab.Controller.Activate(document);
                view = document.Tag as FormDocView;
            }
            else
            {
                view = new FormDocView();
                view.Name = docKey;
                view.MdiParent = form;
                view.Text = docKey;
                view.UcView.SetGraph(v, flow);
                document = tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
                document.Tag = view;

                view.Show();

            }

            DrawStatus(form, v, view);
        }

        public static void DrawStatus(FormMain form, ViewNode v, FormDocView view)
        {
            v.UsedViewNodes.Where(w => w.CoreVertex != null).ForEach(f =>
            {
                if (form.DicStatus.ContainsKey(f.CoreVertex.Value))
                {
                    f.Status4 = form.DicStatus[f.CoreVertex.Value];
                    view.UcView.UpdateStatus(f);
                }
            });
        }
    }
}


