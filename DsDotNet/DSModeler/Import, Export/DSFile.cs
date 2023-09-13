using MessageBox = System.Windows.Forms.MessageBox;

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public static class DSFile
    {
        public static void ZipDSFolder()
        {
            if (Global.IsLoadedPPT() && !Global.ExportPathDS.IsNullOrEmpty())
            {
                string zipDir = Path.GetDirectoryName(Global.ExportPathDS);
                Repository rp = new(zipDir);
                rp.CompressDirectory();
                _ = MessageBox.Show($"{zipDir}.zip 파일 저장 성공");
            }
        }

        private static void Export()
        {
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
            }

            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            string pptPath = Files.GetLast().First();
            string libDir = Path.GetDirectoryName(pptPath); //동일 디렉토리 경로

            string newFile = Files.GetNewFileName(pptPath, "DS");
            string directory = Path.GetDirectoryName(newFile);

            string dsFile = Path.ChangeExtension(newFile, ".ds");
            string confFile = Path.ChangeExtension(newFile, ".json");

            List<string> dsCpuSys = new()
            {
                dsFile
            };

            Global.ExportPathDS = dsFile;

            Global.ActiveSys.GetRecursiveLoadeds().Iter(s =>
            {
                if (s is Device)
                {
                    _ = ExportLoadedSystem(s, directory);
                }

                if (s is ExternalSystem)
                {
                    string path = ExportLoadedSystem(s, directory);
                    if (path != "")
                    {
                        dsCpuSys.Add(path);
                    }
                }
            });

            File.WriteAllText(dsFile, Global.ActiveSys.ToDsText(false));
            ModelLoader.SaveConfigWithPath(confFile, dsCpuSys);

            SplashScreenManager.CloseForm();
        }

        private static string ExportLoadedSystem(LoadedSystem s, string dirNew)
        {
            string commonDir = "";
            string[] lib = dirNew.ToLower().Split('\\');
            string[] abs = s.AbsoluteFilePath.ToLower().Split('\\');
            DirectoryInfo di = new(dirNew);

            for (int i = 0; i < abs.Length; i++)
            {
                if (lib.Length == i || abs[i] != lib[i])
                {
                    if (lib.Length - 1 != i)
                    {
                        Global.Logger.Error($"{s.AbsoluteFilePath}.pptx " +
                            $"\r\nSystem Library호출은 {di.Parent.FullName} 동일/하위 폴더야 합니다.");
                        return "";
                    }
                    break;
                }
                else
                {
                    commonDir += abs[i] + "\\";
                }
            }

            string relativePath = s.AbsoluteFilePath.ToLower().Replace(commonDir.ToLower(), "");
            string absPath = $"{dirNew}\\{relativePath}.ds";

            _ = Directory.CreateDirectory(Path.GetDirectoryName(absPath));
            s.ReferenceSystem.Name = Path.GetFileNameWithoutExtension(absPath);

            File.WriteAllText(absPath, s.ReferenceSystem.ToDsText(false));

            return absPath;
        }

        public static List<Tuple<string, Color>> ToTextColorDS(string dsText)
        {
            List<Tuple<string, Color>> lst = new();
            string[] textLines = dsText.Split('\n');
            Random r = new();
            Color rndColor = Color.LightGoldenrodYellow;

            List<string> textGroup = new();
            string temp = "";
            _ = textLines.Iter(f =>
            {
                temp += $"\n{f}";

                if (f.StartsWith($"[{TextSystem}") || f.Contains($"[{TextFlow}]")  //[flow] F = {} 한줄제외
                    || f.Contains($"[{TextAddress}]")
                    || f.Contains($"[{TextLayout}]")
                    || f.Contains($"[{TextJobs}]")
                    || f.Contains($"[{TextDevice} ")
                    )
                {
                    textGroup.Add(temp.TrimStart('\n'));
                    temp = "";
                }
            });
            textGroup.Add(temp.TrimStart('\n'));

            textGroup.ForEach(f =>
            {
                rndColor = Color.FromArgb(r.Next(130, 230), r.Next(130, 230), r.Next(130, 230));
                lst.Add(System.Tuple.Create(f, rndColor));
            });

            return lst;
        }




        public static void DrawDSText(FormDocText view)
        {
            Task.Run(async () =>
            {
                await view.DoAsync(tcs =>
                {
                    view.TextEdit.ResetText();
                    int cnt = 0;
                    string dsText = "";
                    foreach (var sys in PcContr.RunCpus.SelectMany(s => s.Systems))
                    {
                        dsText += $"{sys.ToDsText(Global.IsDebug)}\r\n\r\n";
                    }

                    List<Tuple<string, Color>> colorTexts = ToTextColorDS(dsText);
                    foreach (Tuple<string, Color> f in colorTexts)
                    {
                        DsProcessEvent.DoWork(Convert.ToInt32(cnt++ * 1.0 / colorTexts.Count() * 100.0));
                        view.AppendTextColor(f.Item1, f.Item2);
                    }

                    Export();

                    DsProcessEvent.DoWork(100);
                    tcs.SetResult(true);
                });
            });
        }



        internal static void UpdateExprAll(FormMain formMain, bool device)
        {
            var textForm = DocContr.CreateDocExprAllOrSelect(formMain, formMain.TabbedView);
            IEnumerable<LogicStatement> css = LogicTree.GetLogicStatement(device);
            IEnumerable<string> texts = css.Select(cs => cs.GetCommentedStatement().Statement.ToText());

            textForm.TextEdit.AppendText($"{string.Join("\r\n", texts)}");
            textForm.TextEdit.ScrollToCaret();
        }

        internal static void UpdateExpr(
              FormDocText textForm
            , LogicStatement logicStatement)
        {
            CommentedStatement cs = logicStatement.GetCommentedStatement();
            DrawExpr(textForm, cs);
            textForm.TextEdit.ScrollToCaret();
        }

        private static void DrawExpr(FormDocText textForm, CommentedStatement cs)
        {
            IEnumerable<Interface.IStorage> tgts = CoreExtensionsModule.getTargetStorages(cs.statement);
            IEnumerable<Interface.IStorage> srcs = CoreExtensionsModule.getSourceStorages(cs.statement);
            string tgtsTexs = string.Join(", ", tgts.Select(s => $"{s.Name}({s.BoxedValue})"));
            string srcsTexs = string.Join(", ", srcs.Select(s => $"{s.Name}({s.BoxedValue})"));
            string comments = cs.comment.IsNullOrEmpty() ? "Empty expression contact only" : cs.comment;

            textForm.AppendTextColor($"{comments}\n".Replace("$", ""), Color.Goldenrod);


            string txtSt = cs.statement.ToText().Replace("$", "");
            string target = txtSt.Split(':')[0];
            string expr = txtSt.Replace(target, "").TrimStart(':');

            textForm.AppendTextColor($"\r\n\t{target}", Color.Gold);
            textForm.AppendTextColor($"\r\n\t\t{expr}", Color.Gold);

            textForm.AppendTextColor($"\r\n\t{tgtsTexs}\r\n\t\t= {srcsTexs}\r\n", Color.LightGreen);
            textForm.AppendTextColor("\r\n", Color.Gold);
        }
    }

}

