using DevExpress.XtraSplashScreen;
using DSModeler.Form;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Engine.Cpu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.DsTextProperty;
using static Engine.Core.ExpressionModule;
using static Engine.Core.ModelLoaderModule;
using static Engine.Core.SystemToDsExt;

namespace DSModeler
{
    public static class DSFile
    {
        public static void OpenDSFolder()
        {
            if (Global.IsLoadedPPT() && !Global.ExportPathDS.IsNullOrEmpty())
                Process.Start(Path.GetDirectoryName(Global.ExportPathDS));
        }
        private static void Export()
        {
            if (!Global.IsLoadedPPT())
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");

            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            var pptPath = Files.GetLast().First();
            var libDir = Path.GetDirectoryName(pptPath); //동일 디렉토리 경로

            var newFile = Files.GetNewFileName(pptPath, "DS");
            var directory = Path.GetDirectoryName(newFile);

            var dsFile = Path.ChangeExtension(newFile, ".ds");
            var confFile = Path.ChangeExtension(newFile, ".json");

            List<string> dsCpuSys = new List<string>();
            dsCpuSys.Add(dsFile);

            Global.ExportPathDS = dsFile;

            Global.ActiveSys.GetRecursiveLoadeds().ForEach(s =>
            {
                if (s is Device)
                    ExportLoadedSystem(s, directory);
                if (s is ExternalSystem)
                {
                    var path = ExportLoadedSystem(s, directory);
                    if (path != "")
                        dsCpuSys.Add(path);
                }
            });

            File.WriteAllText(dsFile, Global.ActiveSys.ToDsText(false));
            ModelLoader.SaveConfigWithPath(confFile, dsCpuSys);

            SplashScreenManager.CloseForm();
        }

        private static string ExportLoadedSystem(LoadedSystem s, string dirNew)
        {
            string commonDir = "";
            var lib = dirNew.ToLower().Split('\\');  
            var abs = s.AbsoluteFilePath.ToLower().Split('\\');
            DirectoryInfo di = new DirectoryInfo(dirNew);

            for (int i = 0; i < abs.Length ; i++)
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
                    commonDir += abs[i] + "\\";
            }

            var relativePath = s.AbsoluteFilePath.ToLower().Replace(commonDir.ToLower(), "");
            var absPath = $"{dirNew}\\{relativePath}.ds";

            Directory.CreateDirectory(Path.GetDirectoryName(absPath));
            s.ReferenceSystem.Name = Path.GetFileNameWithoutExtension(absPath);

            File.WriteAllText(absPath, s.ReferenceSystem.ToDsText(false));

            return absPath;
        }

        public static List<Tuple<string, Color>> ToTextColorDS(string dsText)
        {
            var lst = new List<Tuple<string, Color>>();
            var textLines = dsText.Split('\n');
            Random r = new Random();
            Color rndColor = Color.LightGoldenrodYellow;

            List<string> textGroup = new List<string>();
            string temp = "";
            textLines.Iter(f =>
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
            Task.Run(async() =>
            {
                await view.DoAsync(tcs =>
                {
                    view.TextEdit.ResetText();
                    int cnt = 0;
                    string dsText = "";
                    foreach (var sys in PcControl.RunCpus.SelectMany(s => s.Systems))
                        dsText += $"{sys.ToDsText(Global.IsDebug)}\r\n\r\n";

                    var colorTexts = ToTextColorDS(dsText);
                    foreach (var f in colorTexts)
                    {
                        DsProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (colorTexts.Count()) * 100.0));
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
            var textForm = DocControl.CreateDocExprAllOrSelect(formMain, formMain.TabbedView);
            var css = LogicTree.GetLogicStatement(device);
            var texts = css.Select(cs => cs.GetCommentedStatement().Statement.ToText());

            textForm.TextEdit.AppendText($"{String.Join("\r\n", texts)}");
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
            var tgts = CoreExtensionsModule.getTargetStorages(cs.statement);
            var srcs = CoreExtensionsModule.getSourceStorages(cs.statement);
            string tgtsTexs = string.Join(", ", tgts.Select(s => $"{s.Name}({s.BoxedValue})"));
            string srcsTexs = string.Join(", ", srcs.Select(s => $"{s.Name}({s.BoxedValue})"));
            string comments = cs.comment.IsNullOrEmpty() ? "Empty expression contact only" : cs.comment;

            textForm.AppendTextColor($"{comments}\n".Replace("$", ""), Color.Goldenrod);


            var txtSt = cs.statement.ToText().Replace("$", "");
            var target = txtSt.Split(':')[0];
            var expr = txtSt.Replace(target, "").TrimStart(':');

            textForm.AppendTextColor($"\r\n\t{target}", Color.Gold);
            textForm.AppendTextColor($"\r\n\t\t{expr}", Color.Gold);

            textForm.AppendTextColor($"\r\n\t{tgtsTexs}\r\n\t\t= {srcsTexs}\r\n", Color.LightGreen);
            textForm.AppendTextColor("\r\n", Color.Gold);
        }
    }

}

