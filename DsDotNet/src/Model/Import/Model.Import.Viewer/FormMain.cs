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
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        public static FormMain TheMain;

        private DsSystem _mySystem;
        private DsCPU _myCPU;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private IEnumerable<ViewNode> _myViewNodes;
        private string _dsText;
        private bool _ConvertErr = false;
        public Dictionary<Flow, TabPage> _DicMyUI;
        public Dictionary<Flow, TabPage> _DicExUI;
        public Dictionary<Vertex, ViewNode> _DicVertex;
        public string PathPPT;
        public string PathXLS;
        public bool Busy = false;

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
        public UCView SelectedView =>  xtraTabControl_My.SelectedTab.Tag as UCView;


        private void Form1_Load(object sender, EventArgs e)
        {
            EventExternal.ProcessSubscribe();
            EventExternal.MSGSubscribe();
            EventExternal.CPUSubscribe();

            _DicMyUI = new Dictionary<Flow, TabPage>();
            _DicExUI = new Dictionary<Flow, TabPage>();

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
            string[] lstPPTXLS = new string[2];
            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);
                if (extension == ".pptx")
                    lstPPTXLS[0] = file;
                if (extension == ".xlsx")
                    lstPPTXLS[1] = file;
            }

            if (lstPPTXLS[0] != null && lstPPTXLS[1] != null)
            {
                PathPPT = lstPPTXLS[0];
                InitModel(lstPPTXLS[0]);
                PathXLS = lstPPTXLS[1];
                ImportExcel(lstPPTXLS[1]);
            }
            else
            {
                if (lstPPTXLS[0] != null)
                {
                    PathPPT = lstPPTXLS[0];
                    InitModel(lstPPTXLS[0]);
                }
                if (lstPPTXLS[1] != null)
                {
                    if (PathXLS == lstPPTXLS[1])
                        ImportExcel(lstPPTXLS[1]);
                    else
                    {
                        MSGError($"{PathPPT} 모델로 부터 자동생성된 {PathXLS} 파일을 로드 해야 합니다.");
                    }
                }
            }
        }

        public void UpdateProgressBar(int percent)
        {
            progressBar1.Do(() => progressBar1.Value = percent);
        }

        private void InitModel(string path)
        {

            try
            {
                if (UtilFile.BusyCheck()) return;
                Busy = true;

                PathPPT = path;
                progressBar1.Maximum = 100;
                progressBar1.Step = 1;
                progressBar1.Value = 0;

                _ConvertErr = false;
                richTextBox_ds.Clear();
                _DicMyUI.Clear();
                _DicExUI.Clear();

                xtraTabControl_My.TabPages.Clear();
                xtraTabControl_Ex.TabPages.Clear();

                splitContainer1.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = false;
                button_OpenFolder.Visible = false;

                this.Size = new Size(1920, 1000);
                ImportPPT();
            }
            catch
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, $"{PathPPT} 불러오기 실패!!");
                Busy = false;
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.F1) { HelpLoad(); }
            if ((Keys)e.KeyValue == Keys.F5) { ReloadPPT(); }
            if ((Keys)e.KeyValue == Keys.F6) { TestDebug(); }
            if ((Keys)e.KeyValue == Keys.F7) { TestUnitTest(); }
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
            Process.Start(Path.GetDirectoryName(PathXLS));
        }

        private void button_ClearLog_Click(object sender, EventArgs e)
        {
            richTextBox_Debug.Clear();
            richTextBox_Debug.AppendText($"{DateTime.Now} : Log Clear");
        }

        private void button_comfile_Click(object sender, EventArgs e)
        {
            try
            {
                //var helper = ModelParser.ParseFromString2(_dsText, ParserOptions.Create4Simulation());

                //if (CpuLoader.PreCheck())
                //    MSGWarn("언어체크 성공 !!!!");

                ExportTextModel(Color.Transparent, _dsText, true);
            }

            catch (Exception ex)
            {
                MSGWarn("언어체크 실패 !!!!");
                MSGError(ex.Message);
            }
        }
        private void RefreshText()
        {
            _dsText = "";
            var textLines = richTextBox_ds.Text.Split('\n');
            textLines.ForEach(f =>
            {
                var patternHead = "^\\d*;"; // 첫 ; 내용 제거
                var replaceName = System.Text.RegularExpressions.Regex.Replace(f, patternHead, "");

                _dsText += (replaceName + "\n");
            });
            ExportTextModel(Color.Transparent, _dsText, false);
        }
        private void button_HideLine_Click(object sender, EventArgs e)
        {
            RefreshText();
        }
        private async void button_TestStart_Click(object sender, EventArgs e)
        {
            button_TestStart.Enabled = false;
            button_Stop.Enabled = true;
            //await SimSeg.TestStart(_myCPU, _cts);
            await Task.Delay(0);
            _myCPU.Run();
        }
        private void button_Stop_Click(object sender, EventArgs e)
        {
            button_TestStart.Enabled = true;
            button_Stop.Enabled = false;
            //_cts.Cancel();
            //_cts = new CancellationTokenSource();
            _myCPU.Stop();
        }

        private async void button_TestORG_Click(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100);


            });
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            var segHMI = comboBox_Segment.SelectedItem as SegmentHMI;
            if (segHMI == null) return;

            var ucView = SelectedView;
            segHMI.Memory.Reset.SetValue(false);
            segHMI.Memory.Start.SetValue(true);
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            var segHMI = comboBox_Segment.SelectedItem as SegmentHMI;
            if (segHMI == null) return;

            var ucView = SelectedView;
            segHMI.Memory.Start.SetValue(false);
            segHMI.Memory.Reset.SetValue(true);
        }

      
    }
}
