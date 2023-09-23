using Dual.Common.Core;
using Dual.Common.Winform;
using IOMapApi;
using IOMapForModeler;
using System;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using static IOMap.LS.ScanImpl;
using static IOMapApi.MemoryIOApi;
using static IOMapApi.MemoryIOEventImpl;

namespace IOMapViewer
{
    public partial class FormView : DevExpress.XtraEditors.XtraForm
    {
        MemoryIO m = new MemoryIO(@"UnitTest\A");
        

        public FormView()
        {
            InitializeComponent();
        }
        private void ViewForm_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                string processName = "IOMap.LS";
                if (!Process.GetProcessesByName(processName).Any())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = $"{processName}.exe",
                    };
                    // 프로세스 시작
                    using (Process process = Process.Start(startInfo))
                        Debug.WriteLine($"{processName} started.");
                }
            });


            gridControl1.DataSource = m.GetMemoryAsDataTable();
            var evt = HwTagEventModule.CreateHwTagEvent(new string[] { m.Device }, new  List<IHwTag>());
            evt.Subscribe(args =>
            {
                if(args.Value is bool)
                    Debug.WriteLine($"{args}  BitIndex");
                if (args.Value is UInt64)              
                    Debug.WriteLine($"{args}  UInt64Index");
            });
            Task.Run(() => HwTagEventModule.RunTagEvent());
        }

        bool toggle = false;

        private async void FormView_Shown(object sender, EventArgs ee)
        {
            //ScanIO ss = new ScanIO("192.168.0.100:2004", "XGI-CPUUN");

            while (true)
            {
                await Task.Run(async () =>
                {
                    //ss.DoScan();
                    List<byte> bArr;
                    toggle = !toggle;
                    if (toggle)
                        bArr = new List<byte>() { 0xff, 0xff, 0xff, 0xff, 0xff };
                    else
                        bArr = new List<byte>() { 0, 0, 0 };
                    //m.Write(bArr.ToArray(), 5L);
                    Debug.WriteLine(bArr[0].ToString());

                    await this.DoAsync(async (tsc) =>
                    {
                        gridControl1.DataSource = m.GetMemoryAsDataTable();
                        await Task.Delay(100);
                        tsc.SetResult(true);
                    });
                });

                //var dt = gridControl1.DataSource as DataTable;
                ////IEnumerable row 순서보장 확인 필요??
                //var rowsData = MemoryIO.GetMemoryChunkBySize10(testMemory);
                //int rowIndex = 0;
                ////Iter row 순서보장 확인 필요??
                //rowsData.Iter(row =>
                //{
                //    dt.Rows[rowIndex++].ItemArray = row.Select(cell => (object)cell).ToArray();
                //});
            }


        }
    }
}