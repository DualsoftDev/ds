using DevExpress.XtraEditors;
using System;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.RuntimeGeneratorModule;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        readonly Timer timerLongPress = new Timer { Interval = 10 };

        private void InitializationUIControl()
        {
            timerLongPress.Tick += (sender, e) =>
            {
                PcAction.Step(ace_Play);
            };
            btn_StepLongPress.MouseDown += (sender, e) => timerLongPress.Start();
            btn_StepLongPress.MouseUp += (sender, e) => timerLongPress.Stop();
            btn_StepLongPress.Disposed += (sender, e) =>
            {
                timerLongPress.Stop();
                timerLongPress.Dispose();
            };


            LookupEditExt.InitEdit(gle_Log, gleView_Log);
            LookupEditExt.InitEdit(gle_Expr, gleView_Expr);
            LookupEditExt.InitEdit(gle_Device, gleView_Device);

            gle_Log.Properties.DataSource = LogicLog.ValueLogs;


            ratingControl_Speed.EditValue = ControlProperty.GetSpeed();

            comboBoxEdit_RunMode.Properties.Items.AddRange(RuntimePackageList.ToArray());
            var cpuRunMode = DSRegistry.GetValue(K.CpuRunMode);
            comboBoxEdit_RunMode.EditValue = cpuRunMode == null ? RuntimePackage.Simulation : cpuRunMode;

            var RunCountIn = DSRegistry.GetValue(K.RunCountIn);
            spinEdit_StartIn.Properties.MinValue = 1;
            spinEdit_StartIn.EditValue = RunCountIn == null ? 1 : Convert.ToInt32(RunCountIn);
            var RunCountOut = DSRegistry.GetValue(K.RunCountOut);
            spinEdit_StartOut.Properties.MinValue = 1;
            spinEdit_StartOut.EditValue = RunCountOut == null ? 1 : Convert.ToInt32(RunCountOut);

            var ip = DSRegistry.GetValue(K.RunHWIP);
            textEdit_IP.Text = ip == null ? K.RunDefaultIP : ip.ToString();

            var menuExpand = DSRegistry.GetValue(K.LayoutMenuExpand);
            toggleSwitch_menuExpand.IsOn = Convert.ToBoolean(menuExpand);

            var layoutGraphLineType = DSRegistry.GetValue(K.LayoutGraphLineType);
            toggleSwitch_LayoutGraph.IsOn = Convert.ToBoolean(layoutGraphLineType);


        }

    }
}