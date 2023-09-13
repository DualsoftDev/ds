using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;


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
        string zipPath = Path.GetDirectoryName(Global.ExportPathDS) + ".zip";
        byte[] zipBytes = File.ReadAllBytes(zipPath);
        HttpClient client = new() { BaseAddress = new Uri("http://localhost:5000") };
        HttpResponseMessage response = await client.PostAsJsonAsync("api/upload", zipBytes);
        _ = response.IsSuccessStatusCode
            ? MessageBox.Show("Data has uploaded", "succeed")
            : MessageBox.Show($"Error: {response.ReasonPhrase}", "Failed");

        SplashScreenManager.CloseForm();

        return "";
    }


    public static void ExportApp()
    {
        if (!Global.IsLoadedPPT())
        {
            return;
        }

        if (!Global.ExportPathDS.IsNullOrEmpty())
        {
            foreach (System.Windows.Forms.Form frm in Application.OpenForms)
            {
                if (frm.Name == "MainFormHMI")
                {
                    frm.Activate();
                    return;
                }
            }
            MainFormHMI appHMI = new();
            appHMI.LoadHMI(Global.ExportPathDS);
        }
        else
        {
            _ = MBox.Warn("PC Control 내보내기를 먼저 수행하세요");
            return;
        }
    }
}