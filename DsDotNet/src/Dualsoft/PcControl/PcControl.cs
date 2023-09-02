using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.ExpressionModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class PcControl
    {
        public static List<DsCPU> RunCpus = new List<DsCPU>();
        public static Dictionary<TagHW, IEnumerable<ITag>> DicActionIn = new Dictionary<TagHW, IEnumerable<ITag>>();
        public static Dictionary<ITag, TagHW> DicActionOut = new Dictionary<ITag, TagHW>();

        public static Dictionary<TagHW, IEnumerable<ITag>> GetActionInputs(DsSystem sys)
        {
            var actions = new Dictionary<TagHW, IEnumerable<ITag>>();
            var inTags 
                 = sys.Jobs
                      .SelectMany(j => j.DeviceDefs.Select(s => s.InTag))
                      .Where(w => w != null);
          
            inTags
              .GroupBy(g => g.Address)
              .Iter(g => 
              {
                  var names = String.Join(", ", g.Select(s => s.Name));
                  var hwTag = getTagHW(names, g.Key, true);
                  actions.Add(hwTag, g.Select(s=>s));
              });
       
            return actions;
        }
        public static Dictionary<ITag, TagHW> GetActionOutputs(DsSystem sys)
        {
            var actions = new Dictionary<ITag, TagHW>();
            var inTags
                 = sys.Jobs
                      .SelectMany(j => j.DeviceDefs.Select(s => s.OutTag))
                      .Where(w => w != null);

            inTags
              .GroupBy(g => g.Address)
              .Iter(g =>
              {
                  var names = String.Join(", ", g.Select(s => s.Name));
                  var hwTag = getTagHW(names, g.Key, false);
                  g.Iter(s => actions.Add(s, hwTag));
              });

            return actions;
        }

        private static TagHW getTagHW(string name, string address, bool bInput)
        {
            if (address.IsNullOrEmpty())
                MBox.Error($"주소가 없습니다. {name}");

            var tag = new WMXTag(Global.PaixDriver.Conn as WMXConnection, name);
            tag.SetAddress(address);
            tag.IOType = bInput ? TagIOType.Input : TagIOType.Output;

            return tag;
        }


        private static void CreatePcControl()
        {
            if (Global.CpuRunMode.IsPackagePC())
            {
                PcAction.CreateConnect();
                DicActionIn = GetActionInputs(Global.ActiveSys);
                DicActionOut = GetActionOutputs(Global.ActiveSys);
                Global.PaixDriver.Conn.AddMonitoringTags(DicActionIn.Keys);
                Global.PaixDriver.Conn.AddMonitoringTags(DicActionOut.Values);
            }
        }
        public static void UpdateDevice(GridLookUpEdit gDevice)
        {
            gDevice.Do(() =>
            {
                var tags = DicActionIn.Keys.Cast<WMXTag>().ToList();
                tags.AddRange(DicActionOut.Values.Cast<WMXTag>());
                gDevice.Properties.DataSource = tags;
                gDevice.Properties.DisplayMember = "Name";
            });
        }

        public static async Task CreateRunCpuSingle(Dictionary<DsSystem, PouGen> DicPou)
        {
            CreatePcControl();

            List<DsCPU> runCpus = new List<DsCPU>();
            List<CommentedStatement> css = new List<CommentedStatement>();
            int cnt = 0;
            foreach (var cpu in DicPou.Values)
            {
                DsProcessEvent.DoWork(Convert.ToInt32(((cnt++ * 1.0) / DicPou.Values.Count()) * 50 + 50));
                css.AddRange(cpu.CommentedStatements().ToList());
                await Task.Delay(1);
            }

            var passiveCPU =
               new DsCPU(
               css
             , DicPou.Values.Select(s => s.ToSystem())
             , Global.CpuRunMode);

            runCpus.Add(passiveCPU);
            RunCpus = runCpus;
            DsProcessEvent.DoWork(100);
        }


        /// <summary>
        /// ThreadPool.GetMinThreads 제한으로 멀티로 동시에 돌리는게 한계(thread 스위칭으로 더느림)
        /// Active 1 - Passive n개로 돌림  PC의 절반 CPU 활용
        /// </summary>
        /// <returns></returns>
        public static async Task GetRunCpus(Dictionary<DsSystem, PouGen> DicPou)
        {
            await Task.Yield();

            CreatePcControl();

            List<DsCPU> runCpus = new List<DsCPU>();
            //Global.ActiveSys 제외한  PC의 절반 CPU 활용
            var ableCpuCnt = (Environment.ProcessorCount - 1) / 2;

            var devices = DicPou.Values.Where(d => d.ToSystem() != Global.ActiveSys).ToList();
            if (devices.Any()) //1개이상은 외부 Device 존재
            {
                Dictionary<int, List<PouGen>> pous = new Dictionary<int, List<PouGen>>();
                for (int i = 0; i < devices.Count(); i++)
                {
                    var index = i % ableCpuCnt; //cpu 개수 만큼 만듬
                    if (!pous.ContainsKey(index))
                        pous.Add(index, new List<PouGen> { devices[i] });
                    else
                        pous[index].Add(devices[i]);
                }

                foreach (var pouSet in pous.Values)
                {
                    var passiveCPU =
                   new DsCPU(
                    pouSet.SelectMany(s => s.CommentedStatements())
                 , pouSet.Select(d => d.ToSystem())
                 , Global.CpuRunMode);

                    runCpus.Add(passiveCPU);
                }

            }

            var activeCPU = CreateCpu(DicPou[Global.ActiveSys]);
            runCpus.Add(activeCPU);

            RunCpus = runCpus;
        }

        public static DsCPU CreateCpu(PouGen pou)
        {
            var cpu = new DsCPU(
                pou.CommentedStatements(),
                new List<DsSystem>() { pou.ToSystem() },
                Global.CpuRunMode);

            return cpu;
        }

        public static void ClearModel(FormMain frmMain)
        {
            frmMain.Do(() =>
            {
                if (PcControl.RunCpus.Any())
                    PcAction.Reset(frmMain.Ace_Play, frmMain.Ace_HMI);

                PcControl.RunCpus.Iter(cpu => cpu.Dispose());
                RecentDocs.SetRecentDoc(frmMain.TabbedView.Documents.Select(d => d.Caption));

                frmMain.TabbedView.Controller.CloseAll();
                frmMain.TabbedView.Documents.Clear();
                frmMain.LogCountText.Caption = "";
                LogicLog.ValueLogs.Clear();

                Global.ActiveSys = null;

                Tree.ModelTree.ClearSubBtn(frmMain.Ace_System);
                Tree.ModelTree.ClearSubBtn(frmMain.Ace_Device);
                Tree.ModelTree.ClearSubBtn(frmMain.Ace_HMI);
            });
        }

    }
}


