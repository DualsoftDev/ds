using SplashScreen = DevExpress.XtraSplashScreen.SplashScreen;

namespace IOMapViewer.Utils;

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
        string version = $"{asmName.Version.Major}.{asmName.Version.Minor}.{asmName.Version.Build}";
        labelControl_Ver.Text = string.Format(" v{0} ({1})", version
            , File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString());
        //DsSW.version = asmName.Version.ToString();
    }

    private void Timer1_Tick(object sender, EventArgs e)
    {
        AssemblyName[] asmNameDll = Assembly.GetEntryAssembly().GetReferencedAssemblies();
        if (curDll < asmNameDll.Length)
        {
            labelControl_ReferencedAssemblies.Text = $"{asmNameDll[curDll].Name}, Ver{asmNameDll[curDll].Version}";
            curDll++;
        }
    }

    #region Overrides

    public override void ProcessCommand(Enum cmd, object arg)
    {
        base.ProcessCommand(cmd, arg);
    }

    #endregion
}