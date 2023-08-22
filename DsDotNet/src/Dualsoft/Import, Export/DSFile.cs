using DSModeler.Form;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Engine.Cpu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.DsTextProperty;
using static Engine.Core.ExpressionModule;
using static Engine.Core.SystemToDsExt;

namespace DSModeler
{
    public static class DSFile
    {
        public static List<Tuple<string, Color>> ToTextColorDS(string dsText)
        {
            var lst = new List<Tuple<string, Color>>();
            var textLines = dsText.Split('\n');
            Random r = new Random();
            Color rndColor = Color.LightGoldenrodYellow;
            textLines.ToList().ForEach(f =>
            {
                if (f.StartsWith($"[{TextSystem}") || f.Contains($"[{TextFlow}]")  //[flow] F = {} 한줄제외
                    || f.Contains($"[{TextAddress}]")
                    || f.Contains($"[{TextLayout}]")
                    || f.Contains($"[{TextJobs}]")
                    )
                {
                    rndColor = Color.FromArgb(r.Next(130, 230), r.Next(130, 230), r.Next(130, 230));
                }
                lst.Add(System.Tuple.Create(f, rndColor));
            });

            return lst;
        }

        public static void DrawDSText(FormDocText view)
        {
            Task.Run(async () =>
            {
                await view.TextEditDS.DoAsync(async tsc =>
                {
                    view.TextEditDS.ResetText();
                    int cnt = 0;
                    string dsText = "";
                    foreach (var sys in SIMControl.DicPou.Keys)
                        dsText += $"{sys.ToDsText(Global.IsDebug)}\r\n\r\n";

                    var colorTexts = ToTextColorDS(dsText);
                    foreach (var f in colorTexts)
                    {
                        DsProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (colorTexts.Count()) * 100.0));
                        view.AppendTextColor(f.Item1, f.Item2);
                        await Task.Delay(5);
                    }

                    DsProcessEvent.DoWork(100);

                    tsc.SetResult(true);
                });
            });
        }


        internal static void UpdateExpr(
              FormDocText textForm
            , LogicStatement logicStatement)
        {
            CommentedStatement cs = logicStatement.GetCommentedStatement();
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
            textForm.TextEditDS.ScrollToCaret();
        }
    }

}

