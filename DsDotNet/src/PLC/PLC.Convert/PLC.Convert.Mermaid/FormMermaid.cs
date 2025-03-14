using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PLC.Convert.Mermaid
{
    public partial class FormMermaid : Form
    {
        private WebView2 webView;

        public FormMermaid()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private void InitializeWebView()
        {
            // WebView2 컨트롤 생성
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);
            webView.Source = new Uri("about:blank");  // 빈 페이지 로드



            string mermaidText = @"
                graph TD;
                A[Start] --> B{Decision};
                B -- Yes --> C[Process 1];
                B -- No --> D[Process 2];
                C --> E[End];
                D --> E;
            ";


            // Mermaid.js 다이어그램 로드
            webView.NavigationCompleted += (s, e) => LoadMermaidGraph(mermaidText);
            webView.EnsureCoreWebView2Async();
        }

        public void LoadMermaidGraph(string mermaidText)
        {
           

            string htmlContent = GenerateMermaidHtml(mermaidText);
            string tempPath = Path.Combine(Path.GetTempPath(), "mermaid_graph.html");
            File.WriteAllText(tempPath, htmlContent, Encoding.UTF8);

            webView.Source = new Uri(tempPath);
        }

        private string GenerateMermaidHtml(string mermaidCode)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Mermaid Graph</title>
                <script type='module'>
                    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';
                    mermaid.initialize({{ startOnLoad: true }});
                </script>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                        background-color: #f4f4f4;
                    }}
                </style>
            </head>
            <body>
                <div class='mermaid'>
                    {mermaidCode}
                </div>
            </body>
            </html>";
        }
    }
}
