using Dsu.PLCConverter.FS;
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

            //this.Load += async (s, e) => await OpenPLCAsync();
            button_openPLC.Click += async (s, e) => await OpenPLCAsync();
            button_openDir.Click += async (s, e) => await OpenPLCDirAsync();
            button_MelsecConvert.Click += async (s, e) => await OpenPLCMelsecAsync();

            
        }

        /// **PLC 프로그램을 불러와 Mermaid 변환**
        private async Task<Tuple<string, string>> ImportProgramXG5K(string file, bool bExportEdges, bool bUsingComment)
        {

            this.Text = "XG5000 load: " + file;

            Tuple<List<Rung>, ProgramInfo> result = await ImportXG5kPath.LoadAsync(file);
            List<Rung> rungs = result.Item1;
            ProgramInfo infos = result.Item2;

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"LLM/{Path.GetFileNameWithoutExtension(file)}.mmd");

            // **Rung 데이터를 Dictionary<string, List<string>> 형태로 변환**
            var coils = rungs
                .SelectMany(rung => rung.RungExprs.OfType<Terminal>())
                .Where(terminal => terminal.HasInnerExpr);

            if (bExportEdges)
            {
                var mermaidEdges  = MermaidExportModule.ConvertEdges(coils, bUsingComment);
                File.WriteAllText(path.Replace(".mmd", ".mermaid"), mermaidEdges, Encoding.UTF8);
            }

            // **Mermaid 변환 실행**
            var mermaidText = MermaidExportModule.Convert(coils, bUsingComment);

            return Tuple.Create(mermaidText, path);
        }

        private void OpenInitSetting()
        {
            webView.Source = new Uri("https://dualsoft.com");  // 빈 페이지 로드   
        }


        /// **PLC 데이터 불러오기 및 Mermaid 변환 실행**
        private async Task OpenPLCDirAsync()
        {
            try
            {
                OpenInitSetting();
                var files = FileOpenSave.OpenDirFiles();
                if (files == null || files.Count == 0) return;
                foreach (var file in files)
                    await exportMermaid(file, true, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일을 불러오는 중 오류 발생: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task OpenPLCAsync()
        {
            try
            {
                OpenInitSetting();
                var files = FileOpenSave.OpenFiles();
                if (files == null || files.Length == 0) return;

                string file = files.First();
                await exportMermaid(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일을 불러오는 중 오류 발생: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task OpenPLCMelsecAsync()
        {
            try
            {
                OpenInitSetting();
                var files = FileOpenSave.OpenFiles(true, true);
                if (files == null || files.Length == 0) return;

                await ConvertXGI(files);
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일을 불러오는 중 오류 발생: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task exportMermaid(string file, bool exportEdges = false, bool usingComment = false)
        {
            var (mermaidText, path) = await ImportProgramXG5K(file, exportEdges, usingComment);
            // ✅ **Mermaid 파일 저장 (.mmd)**
            File.WriteAllText(path, mermaidText, Encoding.UTF8);

            LoadMermaidGraph(mermaidText);
        }
    }
}
