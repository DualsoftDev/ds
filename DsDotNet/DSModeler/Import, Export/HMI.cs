using AppHMI;
using DevExpress.XtraSplashScreen;
using Dual.Common.Core.FS;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class HMI
{
    public static async Task<string> ExportWebAsync(FormMain formMain)
    {
        if (!Global.IsLoadedPPT())
        {
            Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
            return "";
        }

        SplashScreenManager.ShowForm(typeof(DXWaitForm));
        var zipPath = Path.GetDirectoryName(Global.ExportPathDS) + ".zip";
        var zipBytes = File.ReadAllBytes(zipPath);
        var client = new HttpClient() { BaseAddress = new Uri("http://localhost:5000") };
        HttpResponseMessage response = await client.PostAsJsonAsync("api/upload", zipBytes);
        if (response.IsSuccessStatusCode)
            MessageBox.Show("Data has uploaded", "succeed");
        else
            MessageBox.Show($"Error: {response.ReasonPhrase}", "Failed");
        SplashScreenManager.CloseForm();

        return "";
    }


    public static void ExportApp()
    {
        if (!Global.IsLoadedPPT()) return;
        if (Global.ExportPathDS.IsNullOrEmpty())
        {
            MBox.Warn("PC Control 내보내기를 먼저 수행하세요");
            return;
        }

        foreach (System.Windows.Forms.Form frm in Application.OpenForms)
        {
            if (frm.Name == "MainFormHMI")
            {
                frm.Activate();
                return;
            }
        }
        var appHMI = new MainFormHMI();
        appHMI.LoadHMI(Global.ExportPathDS);
    }
}