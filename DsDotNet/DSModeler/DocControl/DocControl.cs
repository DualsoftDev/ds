

namespace DSModeler.DocControl
{
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
            return tab.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
        }

        public static void CreateDocStart(FormMain formParent, TabbedView tab)
        {
            string docKey = K.DocStartPage;
            _ = CreateDocForm(new FormDocImage(), formParent, tab, docKey);
        }

        public static void CreateDocDS(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT())
            {
                return;
            }

            string docKey = K.DocDS;

            FormDocText formChiild = new();
            FormDocText form = CreateDocForm(formChiild, formParent, tab, docKey) as FormDocText;
            DSFile.DrawDSText(form);
        }

        public static FormDocText CreateDocExprOrSelect(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT())
            {
                return null;
            }

            string docKey = K.DocExpression;

            FormDocText formChiild = new();
            formChiild = CreateDocForm(formChiild, formParent, tab, docKey) as FormDocText;
            formChiild.Activate();
            return formChiild;

        }
        public static FormDocText CreateDocExprAllOrSelect(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT())
            {
                return null;
            }

            string docKey = K.DocExpressionAll;

            FormDocText formChiild = new();
            formChiild = CreateDocForm(formChiild, formParent, tab, docKey) as FormDocText;
            formChiild.Activate();
            return formChiild;
        }

        public static void CreateDocPLCLS(FormMain formParent, TabbedView tab)
        {
            if (!Global.IsLoadedPPT())
            {
                return;
            }

            if (!RuntimeDS.Package.IsPackagePLC())
            {
                _ = MBox.Warn("설정 H/W 에서 PLC를 선택해야 합니다.");
                return;
            }


            string fullpath = PLC.Export();
            string docKey = K.DocPLC;

            _ = Task.Run(async () =>
            {
                //Storages 연결이슈로  새로 준비 
                await formParent.ImportPowerPointWapper(Files.GetLast());
                await formParent.DoAsync(tsc =>
                {
                    FormDocText formChiild = new();
                    _ = CreateDocForm(formChiild, formParent, tab, docKey);
                    formChiild.TextEdit.Text = File.ReadAllText(fullpath);
                    tsc.SetResult(true);
                });
            });
        }


        public static void CreateDocOrSelect(FormMain formParent, ViewNode v)
        {
            if (!Global.IsLoadedPPT())
            {
                return;
            }

            formParent.Do(() =>
            {
                Flow flow = v.Flow.Value;
                string docKey = flow.QualifiedName;

                FormDocView formChiild = new();
                formChiild = CreateDocForm(formChiild, formParent, formParent.TabbedView, docKey) as FormDocView;
                if (formChiild.UcView.MasterNode == null)
                {
                    formChiild.UcView.SetGraph(v, flow, Global.LayoutGraphLineType);
                }
                //상태 및 값 업데이트
                ViewDraw.DrawStatusNValue(v, formChiild);
            });
        }
    }
}


