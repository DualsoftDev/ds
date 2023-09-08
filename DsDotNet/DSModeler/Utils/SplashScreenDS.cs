using DevExpress.XtraSplashScreen;
using System;
using System.Reflection;
using System.Runtime.Versioning;

namespace DSModeler.Utils;
[SupportedOSPlatform("windows")]
public partial class SplashScreenDS : SplashScreen
{
    int curDll = 0;
    public SplashScreenDS()
    {
        InitializeComponent();
        timer1.Tick += Timer1_Tick;
        timer1.Interval = 150;
        timer1.Start();
        var asm = System.Reflection.Assembly.GetEntryAssembly();
        var asmName = asm.GetName();
        var version = $"{asmName.Version.Major}.{asmName.Version.Minor}.{asmName.Version.Build}";
        labelControl_Ver.Text = string.Format(" v{0} ({1})", version
            , System.IO.File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString());
        //DsSW.version = asmName.Version.ToString();
    }

    private void Timer1_Tick(object sender, EventArgs e)
    {
        var asmNameDll = Assembly.GetEntryAssembly().GetReferencedAssemblies();
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
