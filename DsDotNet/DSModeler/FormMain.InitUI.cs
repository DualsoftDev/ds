using Timer = System.Windows.Forms.Timer;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        private readonly Timer timerPress4StepBtn = new() { Interval = 10 };

        private void InitializationUIControl()
        {
            timerPress4StepBtn.Tick += (sender, e) => PcAction.Step(ace_Play);
            btn_StepLongPress.MouseDown += (sender, e) => timerPress4StepBtn.Start();
            btn_StepLongPress.MouseUp += (sender, e) => timerPress4StepBtn.Stop();
            btn_StepLongPress.Disposed += (sender, e) =>
            {
                timerPress4StepBtn.Stop();
                timerPress4StepBtn.Dispose();
            };

            btn_ONPush.MouseDown += (sender, e) => PcAction.SetBit(gle_Device.EditValue as TagHW, true);
            btn_ONPush.MouseUp += (sender, e) => PcAction.SetBit(gle_Device.EditValue as TagHW, false);


            LookupEditExt.InitEdit(gle_Log, gleView_Log);
            LookupEditExt.InitEdit(gle_Expr, gleView_Expr);
            LookupEditExt.InitEdit(gle_Device, gleView_Device);
            LookupEditExt.InitEdit(gle_HW, gleView_HW);

            gle_HW.Properties.DisplayMember = "Name";
            gle_HW.Properties.ValueMember = "Number";

            gle_Log.Properties.DataSource = LogicLog.ValueLogs;
            gle_HW.Properties.DataSource = HwModels.List;
            gle_HW.EditValue = HwModels.GetModelNumberByRegs();

            ratingControl_Speed.EditValue = ControlProperty.GetSpeed();

            comboBoxEdit_RunMode.Properties.Items.AddRange(RuntimePackageList.ToArray());
            object cpuRunMode = DSRegistry.GetValue(RegKey.CpuRunMode);
            comboBoxEdit_RunMode.EditValue = cpuRunMode ?? RuntimePackage.Simulation;

            object RunCountIn = DSRegistry.GetValue(RegKey.RunCountIn);
            spinEdit_StartIn.Properties.MinValue = 1;
            spinEdit_StartIn.EditValue = RunCountIn == null ? 1 : Convert.ToInt32(RunCountIn);
            object RunCountOut = DSRegistry.GetValue(RegKey.RunCountOut);
            spinEdit_StartOut.Properties.MinValue = 1;
            spinEdit_StartOut.EditValue = RunCountOut == null ? 1 : Convert.ToInt32(RunCountOut);

            object ip = DSRegistry.GetValue(RegKey.RunHWIP);
            textEdit_IP.Text = ip == null ? K.RunDefaultIP : ip.ToString();

            object menuExpand = DSRegistry.GetValue(RegKey.LayoutMenuExpand);
            toggleSwitch_menuExpand.IsOn = Convert.ToBoolean(menuExpand);

            object layoutGraphLineType = DSRegistry.GetValue(RegKey.LayoutGraphLineType);
            toggleSwitch_LayoutGraph.IsOn = Convert.ToBoolean(layoutGraphLineType);


        }

    }
}