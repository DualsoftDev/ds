using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using SplashScreen = DevExpress.XtraSplashScreen.SplashScreen;

namespace OPC.DSClient.WinForm;

[SupportedOSPlatform("windows")]
public partial class SplashScreenDS : SplashScreen
{
    private int curDll;

    public SplashScreenDS()
    {
        InitializeComponent();
        timer1.Tick += Timer1_Tick;
        timer1.Interval = 150;
        timer1.Start();
        Assembly asm = Assembly.GetEntryAssembly();
        AssemblyName asmName = asm.GetName();
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        labelControl_Ver.Text = string.Format(" v{0} ({1})", version
            , File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString());
    }

    private void Timer1_Tick(object sender, EventArgs e)
    {
        if (Global.FolderCount > 0 && Global.VariableCount > 0)
        {
            double progress = (double)Global.OpcProcessCount / (Global.FolderCount + Global.VariableCount);
            labelControl_Process.Text = "Connecting... "+progress.ToString("P2"); // 퍼센트 형식으로 표시
        }

        AssemblyName[] asmNameDll = Assembly.GetEntryAssembly().GetReferencedAssemblies();

        if (curDll < asmNameDll.Length)
        {
            labelControl_ReferencedAssemblies.Text = $"{asmNameDll[curDll].Name}, Ver{asmNameDll[curDll].Version}";
            curDll++;
        }
        else
            curDll = 0;
    }


    #region Overrides

    public override void ProcessCommand(Enum cmd, object arg)
    {
        base.ProcessCommand(cmd, arg);
    }

    #endregion
}