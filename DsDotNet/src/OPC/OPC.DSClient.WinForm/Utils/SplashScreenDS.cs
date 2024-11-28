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
        // OPC 서버 탐색 상태 표시
        if (Global.FolderCount <= 0 || Global.VariableCount <= 0)
        {
            labelControl_Process.Text = "Initializing OPC server connection... Please wait.";
            progressBarControl1.Position = 0; // 초기화 상태
            return;
        }

        // 진행 상태 계산
        double progress = (double)Global.OpcProcessCount / (Global.FolderCount + Global.VariableCount);
        if (progress < 1)
        {
            labelControl_Process.Text = $"Connecting to OPC Server... {progress:P2} completed.";
        }
        else
        {
            labelControl_Process.Text = "OPC Server connection successful! Preparing resources...";
        }

        // 진행률 업데이트
        progressBarControl1.Position = (int)(progress * 100);

        // 로드된 어셈블리 정보 표시
        AssemblyName[] asmNameDll = Assembly.GetEntryAssembly().GetReferencedAssemblies();
        if (asmNameDll.Length > 0)
        {
            if (curDll < asmNameDll.Length)
            {
                labelControl_ReferencedAssemblies.Text = $"{asmNameDll[curDll].Name} {asmNameDll[curDll].Version}.";
                curDll++;
            }
            else
            {
                labelControl_ReferencedAssemblies.Text = "All referenced assemblies loaded successfully.";
                curDll = 0; // 초기화
            }
        }
        else
        {
            labelControl_ReferencedAssemblies.Text = "No referenced assemblies found.";
        }
    }



    #region Overrides

    public override void ProcessCommand(Enum cmd, object arg)
    {
        base.ProcessCommand(cmd, arg);
    }

    #endregion
}