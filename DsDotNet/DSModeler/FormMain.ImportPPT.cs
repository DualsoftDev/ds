namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        private bool ImportingPPT = false;
        public async Task ImportPowerPointWapper(string[] lastFiles)
        {
            if (ImportingPPT || Global.BusyCheck())
            {
                return;
            }

            string[] files = lastFiles.IsNullOrEmpty() ? FileOpenSave.OpenFiles() : lastFiles;
            if (files == null)
            {
                return;
            }
            try
            {
                EventCPU.CPUUnsubscribe();
                await Task.Run(async () =>
                {

                    ImportingPPT = true;
                    ClearModel();
                    Files.SetLast(files);
                    System.Collections.Generic.Dictionary<CoreModule.DsSystem, Engine.CodeGenCPU.CpuLoader.PouGen> dicCpu = await PPT.ImportPowerPoint(files, this);
                    if (!dicCpu.Any()) { return; }

                    await PcContr.CreateRunCpuSingle(dicCpu, gle_Device);

                    EventCPU.CPUSubscribe();

                    if (RuntimeDS.Package.IsStandardPC)
                        Global.DsDriver.Start();

                    LogicTree.UpdateExpr(gle_Expr, toggleSwitch_showDeviceExpr.IsOn);

                    Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                    ImportingPPT = false;
                });
            }
            catch (Exception ex)
            {
                ClearModel();
                Global.Logger.Error(ex.Message);
                DocContr.CreateDocStart(this, TabbedView);
            }
            finally
            {
                DsProcessEvent.DoWork(100);
            }
        }


        public void ClearModel()
        {
            this.Do(() =>
            {
                if (PcContr.RunCpus.Any())
                {
                    PcAction.Reset(ace_Play, ace_HMI, gle_Device);
                }

                _ = PcContr.RunCpus.Iter(cpu => cpu.Dispose());
                RecentDocs.SetRecentDoc(TabbedView.Documents.Select(d => d.Caption));

                if (Global.DsDriver != null)
                    PcContr.Stop();

                _ = TabbedView.Controller.CloseAll();
                TabbedView.Documents.Clear();
                LogCountText.Caption = "";
                LogicLog.ValueLogs.Clear();

                Global.ActiveSys = null;
                ImportingPPT = false;

                Tree.ModelTree.ClearSubBtn(ace_System);
                Tree.ModelTree.ClearSubBtn(ace_Device);
                Tree.ModelTree.ClearSubBtn(ace_ExSystem);
                Tree.ModelTree.ClearSubBtn(ace_HMI);
            });
        }
    }
}
