using Engine.CodeGenCPU;
using Engine.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.EdgeExt;
using static Engine.Core.SystemExt;
using static Engine.Core.Interface;
using static Engine.Cpu.CoreExtensionsModule;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ViewModule;
using static Engine.CodeGenCPU.CpuLoader;
using System.Data;
using static Engine.Core.ExpressionModule;
using Engine.Core;
using static Engine.CodeGenCPU.ConvertSystem;
using log4net.Appender;
using DocumentFormat.OpenXml.Wordprocessing;
using Model.Import.Office;
using Color = System.Drawing.Color;
using System.Security.Cryptography;
using System.Net.Security;
using static Engine.Core.ExpressionForwardDeclModule;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        public static FormMain TheMain;

        private DsCPU _SelectedCPU;
        private DsCPU _SelectedDev;
        public Dictionary<DsSystem, DsCPU> _DicCpu = new Dictionary<DsSystem, DsCPU>();
        public Dictionary<DsSystem, IEnumerable<ViewNode>> _DicViews;

        public Dictionary<Vertex, ViewNode> _DicVertex;
        public Dictionary<int, CommentedStatement> _DicStatement;
        public List<string> _PathPPTs = new List<string>();
        public string _PathXLS;
        public bool Busy = false;
        private CancellationTokenSource _cts = new CancellationTokenSource();


        private DsSystem _HelpSystem;
        private DsSystem SelectedSystem => (comboBox_System.SelectedItem as SystemView).System;


        public FormMain()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            TheMain = this;

            splitContainer1.Panel1Collapsed = true;
            splitContainer2.Panel2Collapsed = true;
            button_copy.Visible = false;

            richTextBox_Debug.AppendText($"{DateTime.Now} : *.pptx 를 드랍하면 시작됩니다");

        }
        public UCView SelectedView => xtraTabControl_My.SelectedTab.Tag as UCView;


        private void Form1_Load(object sender, EventArgs e)
        {
            EventExternal.ProcessSubscribe();
            EventExternal.MSGSubscribe();


            //_Demo = ImportCheck.GetDemoModel("test");

            // this.Text = UtilFile.GetVersion();
            this.Size = new Size(500, 500);
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string xlsx = "";
            _PathPPTs.Clear();

            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);
                if (extension == ".pptx")
                    _PathPPTs.Add(file);
                if (extension == ".xlsx")
                    xlsx = file;
            }
            //ppt xls 동시 로딩시
            if (_PathPPTs.Count > 0 && xlsx != "")
            {
                InitModel(_PathPPTs);
                _PathXLS = xlsx;
                ImportExcel(xlsx);
            }
            else
            {
                //ppt 만 로딩시
                if (_PathPPTs.Count > 0 && xlsx == "")
                    InitModel(_PathPPTs);
                //xls 만 로딩시
                if (_PathPPTs.Count == 0 && xlsx != "")
                {
                    if (_PathXLS == xlsx)
                        ImportExcel(xlsx);
                    else
                        MSGError($"모델로 부터 자동생성된 {_PathXLS} 파일을 로드 해야 합니다.");
                }
            }
        }

        public void UpdateProgressBar(int percent)
        {
            progressBar1.Do(() => progressBar1.Value = percent);
        }

        private void InitModel(List<string> paths)
        {

            try
            {
                if (UtilFile.BusyCheck()) return;
                Busy = true;

                progressBar1.Maximum = 100;
                progressBar1.Step = 1;
                progressBar1.Value = 0;

                richTextBox_ds.Clear();

                xtraTabControl_My.TabPages.Clear();
                xtraTabControl_Ex.TabPages.Clear();
                comboBox_Segment.Items.Clear();
                comboBox_System.Items.Clear();

                splitContainer1.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = false;
                button_OpenFolder.Visible = false;

                this.Size = new Size(1920, 1000);
                _cts.Cancel();
                _cts = new CancellationTokenSource();


                _DicCpu.ForEach(cpu => {
                    if (cpu.Value != null && cpu.Value.IsRunning)
                        cpu.Value.Stop();
                });

                ImportPowerPoint(paths);


                button_CreateExcel.Visible = true;
                pictureBox_xls.Visible = true;
                button_TestORG.Visible = true;
                button_copy.Visible = false;
            }
            catch
            {
                Busy = false;
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.F1) { HelpLoad(); }
            if ((Keys)e.KeyValue == Keys.F5) { ReloadPPT(); }
            if ((Keys)e.KeyValue == Keys.F7) { TestDebug(); }
            if ((Keys)e.KeyValue == Keys.F8) { RefreshGraph(); }
        }
        private void button_copy_Click(object sender, EventArgs e)
        {
            var text = richTextBox_ds.Text;
            RichTextBoxExtensions.SetClipboard(text);
            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"클립보드복사 성공!! Ctrl+V로 붙여넣기 가능합니다.");
        }

        private void button_CreateExcel_Click(object sender, EventArgs e)
        {
            ExportExcel();
        }
        private void button_OpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start(Path.GetDirectoryName(_PathXLS));
        }

        private void button_ClearLog_Click(object sender, EventArgs e)
        {
            richTextBox_Debug.Clear();
            richTextBox_Debug.AppendText($"{DateTime.Now} : Log Clear");
        }

        private  void button_TestStart_Click(object sender, EventArgs e)
        {
            StartResetBtnUpdate(true);
        }
        private void button_Stop_Click(object sender, EventArgs e)
        {
            StartResetBtnUpdate(false);
            _DicCpu.Values.ForEach(f=>f.Stop());
        }

        private async void button_TestORG_Click(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100);
            });
        }
        private void StartResetBtnUpdate(bool Start)
        {
            button_TestStart.Enabled = !Start;
            button_Stop.Enabled = Start;
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            var segHMI = comboBox_Segment.SelectedItem as SegmentHMI;
            if (segHMI == null) return;

            var ucView = SelectedView;
            segHMI.VertexM.RT.Value = false;
            segHMI.VertexM.ST.Value = true;
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            var segHMI = comboBox_Segment.SelectedItem as SegmentHMI;
            if (segHMI == null) return;

            var ucView = SelectedView;
            segHMI.VertexM.RT.Value = true;
            segHMI.VertexM.ST.Value = false;
        }


        private void UpdateCpuUI(IEnumerable<string> text)
        {
            comboBox_Segment.DisplayMember = "Display";

            if (comboBox_Segment.Items.Count > 0)
                comboBox_Segment.SelectedIndex = 0;

            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"\r\n{string.Join("\r\n", text)}");
        }


        internal void UpdateGraphUI(IEnumerable<ViewNode> lstViewNode)
        {
            xtraTabControl_My.TabPages.Clear();
            lstViewNode.ForEach(f =>
            {
                var flow = f.Flow.Value;
                UCView viewer = new UCView { Dock = DockStyle.Fill };
                viewer.SetGraph(f, f.Flow.Value);

                TabPage tab = new TabPage();
                tab.Controls.Add(viewer);
                tab.Tag = viewer;
                tab.Text = $"{flow.System.Name}.{flow.Name}({f.Page})";
                this.Do(() =>
                {
                    xtraTabControl_My.TabPages.Add(tab);
                    xtraTabControl_My.SelectedTab = tab;
                });
            });
        }

        private IEnumerable<DsSystem> GetSystems()
        {
            List<SystemView> sysViews = new List<SystemView>();
            foreach (SystemView systemView in comboBox_System.Items)
                sysViews.Add(systemView);
            return sysViews.Select(s => s.System);
        }
        private void UpdateDevice(SystemView sysView)
        {
            var devices = sysView.System.GetRecursiveSystems();
            comboBox_Device.Items.Clear();
            foreach (var dev in devices)
            {
                var vi = new SystemView() { Display = dev.Name, System = dev, ViewNodes = _DicViews[dev].ToList() };
                comboBox_Device.Items.Add(vi);
            }

            comboBox_Device.DisplayMember = "Display";
            comboBox_Device.SelectedIndex = 0;
        }

        private void comboBox_Device_SelectedIndexChanged(object sender, EventArgs e)
        {
            SystemView sysView = comboBox_Device.SelectedItem as SystemView;
            _SelectedDev = _DicCpu[sysView.System];
            UpdateSelectedCpu(sysView);
            checkedListBox_Ex.Items.Clear();
            checkedListBox_Ex.DisplayMember = "Display";
            checkedListBox_Ex.ItemCheck += (ss, ee) => { if (checkedListBox_Ex.Enabled) ee.NewValue = ee.CurrentValue; };

        }
        private void comboBox_System_SelectedIndexChanged(object sender, EventArgs e)
        {
            SystemView sysView = comboBox_System.SelectedItem as SystemView;
            _SelectedCPU = _DicCpu[sysView.System];
            UpdateDevice(sysView);
            UpdateSelectedCpu(sysView);
            checkedListBox_My.Items.Clear();
            checkedListBox_My.DisplayMember = "Display";
            checkedListBox_My.ItemCheck += (ss, ee) => { if (checkedListBox_My.Enabled) ee.NewValue = ee.CurrentValue; };

        }

        public void UpdateLogComboBox(IStorage storage, object value,  DsCPU cpu)
        {
            var name = value is bool ? storage.Name : $"{storage.Name}({value})";
            var onOff = value is bool ? Convert.ToBoolean(value) : false;
            var sd = new StorageDisplay() { Display = name, Storage = storage, Value = value, OnOff = onOff };
            if (_SelectedCPU == cpu)
            {
                checkedListBox_My.Enabled= false;
                checkedListBox_My.Items.Add(sd, sd.OnOff);
                checkedListBox_My.SelectedIndex = checkedListBox_My.Items.Count - 1;
                checkedListBox_My.Enabled= true;
            }
            if (_SelectedDev == cpu)
            {
                checkedListBox_Ex.Enabled= false;
                checkedListBox_Ex.Items.Add(sd, sd.OnOff);
                checkedListBox_Ex.SelectedIndex = checkedListBox_Ex.Items.Count - 1;
                checkedListBox_Ex.Enabled= true;
            }
        }
        private void checkedListBox_My_DoubleClick(object sender, EventArgs e)
        {
            StorageDisplay sd = checkedListBox_My.SelectedItem as StorageDisplay;
            _SelectedCPU.CommentedStatements
                .Where(cs => getTargetStorages(cs.Statement).ToList().Contains(sd.Storage))
                .ForEach(cs => ShowExpr(cs));

        }

    private void checkedListBox_Ex_DoubleClick(object sender, EventArgs e)
        {

        }

        private void UpdateSelectedCpu(SystemView sysView)
        {
            _DicVertex = new Dictionary<Vertex, ViewNode>();
            comboBox_Segment.Items.Clear();

            _DicStatement = new Dictionary<int, CommentedStatement>();
            comboBox_TestExpr.Items.Clear();

            sysView.System.GetVertices()
                .ForEach(v =>
                {
                    var nodes = sysView.ViewNodes.SelectMany(s => s.UsedViewNodes);
                    var viewNode = nodes.Where(w => w.CoreVertex != null)
                                        .First(w => w.CoreVertex.Value == v);

                    _DicVertex.Add(v, viewNode);
                    if (v is Real)
                    {
                        comboBox_Segment.Items
                        .Add(new SegmentHMI { Display = v.QualifiedName, Vertex = v, ViewNode = viewNode, VertexM = v.TagManager as VertexManager });
                    }
                });
            int cnt = 0;
            var text = _DicCpu[sysView.System].CommentedStatements.Select(rung =>
            {
                var description = rung.comment;
                var statement = rung.statement;
                var targetValue = rung.TargetValue;
                _DicStatement.Add(cnt, rung);
                comboBox_TestExpr.Items.Add(cnt);
                return $"{cnt++}\t[{targetValue}] Spec:{description.Replace("%", " ").Replace("$", " ")}";
            });

            StartResetBtnUpdate(true);

            UpdateCpuUI(text);
            UpdateGraphUI(sysView.ViewNodes);

            DisplayTextModel(System.Drawing.Color.Transparent, sysView.System.ToDsText());

        }


        private void comboBox_TestExpr_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBox_TestExpr.SelectedIndex;
            var cs = _DicStatement[index];
            // cs.statement.Do();
            ShowExpr(cs);
        }

        private void ShowExpr(CommentedStatement cs)
        {
            var tgts = getTargetStorages(cs.statement);
            var srcs = getSourceStorages(cs.statement);
            string tgtsTexs = string.Join(", ", tgts.Select(s => $"{s.Name}({s.BoxedValue})"));
            string srcsTexs = string.Join(", ", srcs.Select(s => $"{s.Name}({s.BoxedValue})"));

            richTextBox_ds.AppendTextColor($"{cs.comment}".Replace("$", ""), Color.White);
            richTextBox_ds.AppendTextColor($"\r\n\t{tgtsTexs} = {srcsTexs}\r\n", Color.White);
            richTextBox_ds.AppendTextColor("\r\n", Color.White);
            richTextBox_ds.ScrollToCaret();
        }
    }
}
