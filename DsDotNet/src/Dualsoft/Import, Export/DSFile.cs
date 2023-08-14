using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraEditors;
using DSModeler.Form;
using Dual.Common.Core.FS;
using Dual.Common.Winform;
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

        public static void DrawDSText(FormMain form, FormDocText view)
        {
            Task.Run(async () =>
            {
                await view.TextEditDS.DoAsync(async tsc =>
                {
                    view.TextEditDS.ResetText();
                    int cnt = 0;
                    string dsText = "";
                    foreach (var sys in form.DicCpu.Keys)
                        dsText += $"{sys.ToDsText()}\r\n\r\n";

                    var colorTexts = ToTextColorDS(dsText);
                    foreach (var f in colorTexts)
                    {
                        ProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (colorTexts.Count()) * 100.0));
                        view.AppendTextColor(f.Item1, f.Item2);
                        await Task.Delay(5);
                    }

                    ProcessEvent.DoWork(100);

                    tsc.SetResult(true);
                });
            });
        }

        public static void UpdateExpr
            (Dictionary<string, CommentedStatement> dicStatement
            , FormDocText textForm
            , ComboBoxEdit comboBoxEdit_Expr)
        {
         
            string num = comboBoxEdit_Expr.SelectedItem.ToString().Split(';')[0];
            CommentedStatement cs = dicStatement[num];
            var tgts = CoreExtensionsModule.getTargetStorages(cs.statement);
            var srcs = CoreExtensionsModule.getSourceStorages(cs.statement);
            string tgtsTexs = string.Join(", ", tgts.Select(s => $"{s.Name}({s.BoxedValue})"));
            string srcsTexs = string.Join(", ", srcs.Select(s => $"{s.Name}({s.BoxedValue})"));
            string comments = cs.comment.IsNullOrEmpty() ? "Empty expression contact only" : cs.comment;

            textForm.AppendTextColor($"{num} : {comments}\n".Replace("$", ""), Color.Gold);
            textForm.AppendTextColor($"\r\n\t{cs.statement.ToText().Replace("$", "")} ", Color.Gold);
            textForm.AppendTextColor($"\r\n\t{tgtsTexs} = {srcsTexs}\r\n", Color.LightGreen);
            textForm.AppendTextColor("\r\n", Color.Gold);
            textForm.TextEditDS.ScrollToCaret();
        }

       
    }

}

