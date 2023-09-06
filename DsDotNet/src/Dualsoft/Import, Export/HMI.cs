using DevExpress.XtraSplashScreen;
using System.Text;
using Engine.CodeGenHMI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using DevExpress.ClipboardSource.SpreadsheetML;
using System.Runtime.InteropServices.ComTypes;
using static Engine.Core.CoreModule;
using Dual.Common.Core.FS;
using System.Collections.Generic;
using static Engine.Core.DsText;
using static Engine.Core.SystemToDsExt;
using Engine.Core;
using Engine.Parser.FS;
using Microsoft.Msagl.GraphViewerGdi;
using static Engine.Parser.FS.ParserOptionModule;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraRichEdit.Import.Html;
using LanguageExt.Pipes;
using System.Net.Http.Json;

namespace DSModeler
{
    public static class HMI
    {
        public static async Task<string> ExportAsync(FormMain formMain)
        {
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
                return "";
            }
            
            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            var zipPath = Path.GetDirectoryName(Global.ExportPathDS) + ".zip";
            var zipBytes = File.ReadAllBytes(zipPath);
            var client = new HttpClient() { BaseAddress = new Uri("https://localhost:5001") };
            HttpResponseMessage response = await client.PostAsJsonAsync("api/upload", zipBytes);
            if (response.IsSuccessStatusCode)
                MessageBox.Show("Data successfully sent", "succeed");
            else
                MessageBox.Show($"Error: {response.ReasonPhrase}", "Failed");
            SplashScreenManager.CloseForm();

            return "";
        }
    }
}