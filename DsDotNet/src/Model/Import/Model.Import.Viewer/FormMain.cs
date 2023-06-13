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
using static Engine.Core.ExpressionModule;
using Engine.Core;
using Color = System.Drawing.Color;
using static Engine.CodeGenCPU.SystemManagerModule;
using static Engine.CodeGenCPU.ConvertCoreExt;
using static Engine.Common.FS.CollectionAlgorithm;
using static Model.Import.Office.ImportPPTModule;
using static Engine.Core.RuntimeGeneratorModule;
using Engine.Common.FS;
using static Engine.Core.TagModule;
using static Engine.Core.TagKindModule;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        public static FormMain TheMain;

        private DsCPU _SelectedCPU;
        private DsCPU _SelectedDev;
        public Dictionary<DsSystem, DsCPU> _DicCpu = new Dictionary<DsSystem, DsCPU>();
        public Dictionary<DsSystem, IEnumerable<ViewNode>> _DicViews;
        public IEnumerable<PptResult> _PPTResults;

        public Dictionary<Vertex, ViewNode> _DicVertex;
        public Dictionary<int, CommentedStatement> _DicStatement;
        public List<string> _PathPPTs = new List<string>();
        public string _ResultDirectory = "";
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

            checkedListBox_My.ItemCheck += (ss, ee) => { if (checkedListBox_My.Enabled) ee.NewValue = ee.CurrentValue; };
            checkedListBox_Ex.ItemCheck += (ss, ee) => { if (checkedListBox_Ex.Enabled) ee.NewValue = ee.CurrentValue; };
            checkedListBox_My.DisplayMember = "Display";
            checkedListBox_Ex.DisplayMember = "Display";
            checkedListBox_sysHMI.DisplayMember = "Display";
            comboBox_System.DisplayMember = "Display";
            comboBox_Segment.DisplayMember = "Display";

            DU.enumValues(typeof(RuntimePackage)).Cast<RuntimePackage>()
                .ForEach(f => comboBox_Package.Items.Add(f));
            comboBox_Package.SelectedIndex = 0;
        }

        public void createSysHMI(DsSystem sys)
        {
            checkedListBox_sysHMI.Items.Clear();
            var sysBits = Enum.GetValues(typeof(SystemTag)).Cast<SystemTag>();
            sysBits
                .Where(w => !w.HasFlag(SystemTag.on) && !w.HasFlag(SystemTag.off))     //시스템 비트  제외
                .Select(f=> TagInfoType.GetTagSys(sys, f))
                .OfType<PlanVar<bool>>()
                .ForEach(tag =>
                {
                var sd = new StorageDisplay() { Display = tag.Name, Storage = tag, Value = tag.Value, OnOff = Convert.ToBoolean(tag.Value) };
                checkedListBox_sysHMI.Items.Add(sd);
                checkedListBox_sysHMI.SetItemChecked(checkedListBox_sysHMI.Items.Count - 1, sd.OnOff);
            });
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
                ImportPPTsXls(_PathPPTs, xlsx);
            else
            {
                //ppt 만 로딩시
                if (_PathPPTs.Count > 0 && xlsx == "")
                    ImportPPTs(_PathPPTs);
                //xls 만 로딩시
                if (_PathPPTs.Count == 0 && xlsx != "")
                    ImportExcel(xlsx);
            }
        }

        public void UpdateProgressBar(int percent)
        {
            progressBar1.Do(() => progressBar1.Value = percent);
        }

        private void ImportPPTsXls(List<string> pptPaths, string xlsPath)
        {
            ImportPPTs(pptPaths);
            ImportExcel(xlsPath);
            var plcPath = Path.ChangeExtension(UtilFile.GetNewPathXls(pptPaths), "xml");
            Directory.CreateDirectory(Path.GetDirectoryName(plcPath));
            ExportPLC($"{Path.GetDirectoryName(plcPath)}\\DSLogic{DateTime.Now.ToString("(HH-mm-ss)")}.xml");
        }

        private void ImportPPTs(List<string> paths)
        {

            try
            {
                if (UtilFile.BusyCheck()) return;

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

                this.Size = new Size(1600, 1000);
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
                button_CreatePLC.Visible = false;
            }
            catch
            {
                ProcessEvent.DoWork(0);
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.F1) { HelpLoad(); }
           // if ((Keys)e.KeyValue == Keys.F5) { ReloadPPT(); }
           // if ((Keys)e.KeyValue == Keys.F6) { TestDebug(false, false); }
            if ((Keys)e.KeyValue == Keys.F7) { TestDebug(true, false); }
            if ((Keys)e.KeyValue == Keys.F8) { TestDebug(false, true); }
            if ((Keys)e.KeyValue == Keys.F9) { RefreshGraph(); }
        }
        private void button_copy_Click(object sender, EventArgs e)
        {
            var text = richTextBox_ds.Text;
            RichTextBoxExtensions.SetClipboard(text);
            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"클립보드복사 성공!! Ctrl+V로 붙여넣기 가능합니다.");
        }

        private void button_CreateExcel_Click(object senderㅇ, EventArgs e)
        {
            ExportExcel();
        }
        private void button_OpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start(_ResultDirectory);
        }

        private void button_ClearLog_Click(object sender, EventArgs e)
        {
            ClearLog();
        }

        private void ClearLog()
        {
            richTextBox_Debug.Clear();
            richTextBox_Debug.AppendText($"{DateTime.Now} : Log Clear");
            checkedListBox_My.Items.Clear();
            checkedListBox_My.Enabled = false;
            checkedListBox_Ex.Items.Clear();
            checkedListBox_Ex.Enabled = false;
        }

        private  void button_TestStart_Click(object sender, EventArgs e)
        {
            StartResetBtnUpdate(true);
            _DicCpu.Values.ForEach(f=>f.Run());
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
            var segHMI = comboBox_Segment.SelectedItem as SegmentView;
            if (segHMI == null) return;
            if (segHMI.VertexM == null) return;
            var ucView = SelectedView;
            segHMI.VertexM.RT.Value = false;
            segHMI.VertexM.ST.Value = true;
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            var segHMI = comboBox_Segment.SelectedItem as SegmentView;
            if (segHMI == null) return;
            if (segHMI.VertexM == null) return;

            var ucView = SelectedView;
            segHMI.VertexM.RT.Value = true;
            segHMI.VertexM.ST.Value = false;
        }


        private void UpdateCpuUI(IEnumerable<string> text)
        {

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
            if (devices.Any()) comboBox_Device.SelectedIndex = 0;
        }

        private void comboBox_Device_SelectedIndexChanged(object sender, EventArgs e)
        {
            SystemView sysView = comboBox_Device.SelectedItem as SystemView;
            _SelectedDev = _DicCpu[sysView.System];
            UpdateSelectedCpu(sysView);

        }
        private void comboBox_System_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSystemUI();
        }

        private void UpdateSystemUI()
        {
            SystemView sysView = comboBox_System.SelectedItem as SystemView;
            if (_DicCpu.ContainsKey(sysView.System))
            {
                _SelectedCPU = _DicCpu[sysView.System];
                UpdateDevice(sysView);
                UpdateSelectedCpu(sysView);

                createSysHMI(sysView.System);
                UpdatecomboBox_SegmentHMI(sysView);
            }
            else
            {
                UpdateSelectedView(sysView);
            }
        }

        public void UpdateLogComboBox(IStorage storage, object value, DsCPU cpu)
        {
            this.Do(() =>
            {

                var name = value is bool ? storage.Name : $"{storage.Name}({value})";
                var onOff = value is bool ? Convert.ToBoolean(value) : false;
                var sd = new StorageDisplay() { Display = name, Storage = storage, Value = value, OnOff = onOff };
                if (_SelectedCPU == cpu)
                {
                    checkedListBox_My.Enabled = false;
                    checkedListBox_My.Items.Add(sd, sd.OnOff);
                    checkedListBox_My.SelectedIndex = checkedListBox_My.Items.Count - 1;
                    checkedListBox_My.Enabled = true;
                }
                if (_SelectedDev == cpu)
                {
                    checkedListBox_Ex.Enabled = false;
                    checkedListBox_Ex.Items.Add(sd, sd.OnOff);
                    checkedListBox_Ex.SelectedIndex = checkedListBox_Ex.Items.Count - 1;
                    checkedListBox_Ex.Enabled = true;
                }
            });
        }
        private void checkedListBox_My_DoubleClick(object sender, EventArgs e)
        {
            StorageDisplay sd = checkedListBox_My.SelectedItem as StorageDisplay;
            Debug.Assert(sd != null);

            displayExprLog(sd.Storage);
        }
        private void checkedListBox_Ex_DoubleClick(object sender, EventArgs e)
        {
            StorageDisplay sd = checkedListBox_Ex.SelectedItem as StorageDisplay;
            Debug.Assert(sd != null);
            displayExprLog(sd.Storage);
        }

        private void displayExprLog(IStorage storage)
        {
            var exprs =
                _SelectedCPU.CommentedStatements
                    .Where(cs => getTargetStorages(cs.Statement).ToList().Contains(storage));

            if (exprs.Length() > 0)
            {
                exprs.ForEach(cs => ShowExpr(cs));
            }
            else
            {
                richTextBox_ds.AppendTextColor($"\r\n{storage.ToText()} value : {storage.BoxedValue}     조건전용 신호\r\n", Color.WhiteSmoke);
                richTextBox_ds.ScrollToCaret();
            }
        }



        private void checkedListBox_sysHMI_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            StorageDisplay sd = checkedListBox_sysHMI.Items[e.Index] as StorageDisplay;
            if (sd == null) return;
            sd.Storage.BoxedValue = e.NewValue == CheckState.Checked ? true : false;
        }



        private void UpdateSelectedView(SystemView sysView)
        {
            _DicVertex = new Dictionary<Vertex, ViewNode>();
            sysView.System.GetVertices()
                .ForEach(v =>
                {
                    var nodes = sysView.ViewNodes.SelectMany(s => s.UsedViewNodes);
                    var viewNode = nodes.Where(w => w.CoreVertex != null)
                                        .First(w => w.CoreVertex.Value == v);

                    _DicVertex.Add(v, viewNode);
                });


            StartResetBtnUpdate(true);

            UpdateGraphUI(sysView.ViewNodes);

            DisplayTextModel(System.Drawing.Color.Transparent, sysView.System.ToDsText());
        }

        private void UpdatecomboBox_SegmentHMI(SystemView sysView)
        {
            comboBox_Segment.Items.Clear();

            sysView.System.GetVertices()
                .ForEach(v =>
                {
                    var nodes = sysView.ViewNodes.SelectMany(s => s.UsedViewNodes);
                    var viewNode = nodes.Where(w => w.CoreVertex != null)
                                        .First(w => w.CoreVertex.Value == v);

                    if (v is Real)
                    {
                        comboBox_Segment.Items
                        .Add(new SegmentView { Display = v.QualifiedName, Vertex = v, ViewNode = viewNode, VertexM = v.TagManager as VertexManager });
                    }
                });
        }

        private void UpdateSelectedCpu(SystemView sysView)
        {

            _DicStatement = new Dictionary<int, CommentedStatement>();
            comboBox_TestExpr.Items.Clear();


            int cnt = 0;
            List<string> lstText = new List<string>();
             _DicCpu[sysView.System].CommentedStatements.ForEach(rung =>
            {
                var description = rung.comment;
                var statement = rung.statement;
                var targetValue = rung.TargetValue;
                _DicStatement.Add(cnt, rung);
                comboBox_TestExpr.Items.Add(cnt);
                lstText.Add( $"{cnt++}\t[{targetValue}] Spec:{description.Replace("%", " ").Replace("$", " ")}");
            });

            StartResetBtnUpdate(true);
            UpdateCpuUI(lstText);

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
            richTextBox_ds.AppendTextColor($"{cs.comment}".Replace("$", ""), Color.Gold);
            richTextBox_ds.AppendTextColor($"\r\n\t{cs.statement.ToText()} ", Color.Gold);
            richTextBox_ds.AppendTextColor($"\r\n\t{tgtsTexs} = {srcsTexs}\r\n", Color.LightGreen);
            richTextBox_ds.AppendTextColor("\r\n", Color.Gold);
            richTextBox_ds.ScrollToCaret();
        }

        private void comboBox_Package_SelectedIndexChanged(object sender, EventArgs e)
        {
            Runtime.Package = comboBox_Package.SelectedItem as RuntimePackage;
        }

        private void button_CreatePLC_Click(object sender, EventArgs e)
        {
            ExportPLC($"{_ResultDirectory}\\DSLogic{DateTime.Now.ToString("(HH-mm-ss)")}.xml");


        }
    }
}
