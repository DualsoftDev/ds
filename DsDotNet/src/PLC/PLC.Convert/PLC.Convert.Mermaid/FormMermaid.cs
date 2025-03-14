using Microsoft.Web.WebView2.WinForms;
using PLC.Convert.FS;
using PLC.Convert.LSCore;
using PLC.Convert.LSCore.Expression;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLC.Convert.Mermaid
{
    public partial class FormMermaid : Form
    {
        public FormMermaid()
        {
            InitializeComponent();
            InitializeWebView();

            this.Load += async (s, e) => await OpenPLCAsync();
            button_openPLC.Click += async (s, e) => await OpenPLCAsync();
        }

        /// **PLC 프로그램을 불러와 Mermaid 변환**
        private async Task<Tuple<string, string>> ImportProgramXG5K()
        {
            var files = FileOpenSave.OpenFiles();
            if (files == null || files.Length == 0) return null;

            string file = files.First();
            this.Text = "XG5000 load: " + file;

            Tuple<List<Rung>, ProgramInfo> result = await ImportXG5kPath.LoadAsync(file);
            List<Rung> rungs = result.Item1;
            ProgramInfo infos = result.Item2;

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"LLM/{Path.GetFileNameWithoutExtension(file)}.mmd");

            // **Rung 데이터를 Dictionary<string, List<string>> 형태로 변환**
            var coilDictionary = rungs
                .SelectMany(rung => rung.RungExprs.OfType<Terminal>())
                .Where(terminal => terminal.HasInnerExpr)
                .DistinctBy(terminal => terminal.Name)
                .ToDictionary(
                    coil => coil.Name,
                    coil => GetContactPositiveNames(coil)
                        .Select(t => t.Name)
                        .ToList()
                );

            // **Mermaid 변환 실행**
            var mermaidText = MermaidExportModule.Convert(coilDictionary);
            return Tuple.Create(mermaidText, path);
        }

        /// **양극성(Positive) Contact 추출**
        public List<Terminal> GetContactPositiveNames(Terminal terminal)
        {
            var contactTerminals = terminal.InnerExpr.GetTerminals().ToList();
            var expressionText = terminal.InnerExpr.ToText();
            return ConvertPLCModule.getContactNamesForCSharp(contactTerminals, new List<Terminal>(), expressionText, 0, 3, true);
        }
            

        ///// **Contact 탐색 (재귀적으로 내부 Expression 검사)**
        //public List<Terminal> GetContactNames(List<Terminal> contactTerminals, List<Terminal> contactUnits, string expressionText, int depth, int maxDepth, bool bPositive = true)
        //{
        //    if (depth >= maxDepth) return contactUnits;  // **최대 깊이 도달 시 종료**

        //    foreach (var terminal in contactTerminals)
        //    {
        //        if (contactUnits.Any(c => c.Name == terminal.Name)) continue; // **중복 방지**

        //        bool isPositive = !expressionText.Contains($"!{terminal.Name}");

        //        if (isPositive == bPositive && !LLMKey.KeySkipName.Any(terminal.Name.Contains))
        //        {
        //            contactUnits.Add(terminal);  // **유효한 터미널 추가**

        //            // **재귀적으로 내부 터미널 탐색**
        //            if (terminal.InnerExpr != null)
        //            {
        //                GetContactNames(terminal.InnerExpr.GetTerminals().ToList(), contactUnits, terminal.InnerExpr.ToText(), depth + 1, maxDepth);
        //            }
        //        }
        //    }

        //    return contactUnits;
        //}

        /// **PLC 데이터 불러오기 및 Mermaid 변환 실행**
        private async Task OpenPLCAsync()
        {
            try
            {
                var (mermaidText, path) = await ImportProgramXG5K();
                // ✅ **Mermaid 파일 저장 (.mmd)**
                File.WriteAllText(path, mermaidText, Encoding.UTF8);

                LoadMermaidGraph(mermaidText);
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일을 불러오는 중 오류 발생: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
