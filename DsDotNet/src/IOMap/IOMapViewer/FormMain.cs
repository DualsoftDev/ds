

using Dual.Common.Core;
using IOMapViewer.DocControl;
using IOMapViewer.Utils;

namespace IOMapViewer
{
    [SupportedOSPlatform("windows")]
    public partial class FormMain : XtraForm
    {

        public TabbedView TabbedView => tabbedView_doc;
        public BarStaticItem LogCountText => barStaticItem_logCnt;




        public FormMain()
        {
            InitializeComponent();
            KeyPreview = true;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"IOMapViewer v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);

            InitializationLogger();
            if (!Global.IsDebug)
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
            }
        }

        private void InitializationLogger()
        {
            ucLog1.InitLoad();
            Log4NetLogger.AppendUI(ucLog1);
            Global.Logger.Info($"Starting Dualsoft v{Global.AppVersion}");
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            DocContr.CreateDocDS(this, TabbedView);

        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                LayoutForm.SaveLayout(dockManager);
            }
        }



       
    }
}