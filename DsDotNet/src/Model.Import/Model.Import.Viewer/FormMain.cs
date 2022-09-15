using Engine;
using Engine.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static Engine.Common.FS.MessageEvent;
using static Model.Import.Office.Model;
using static Model.Import.Office.Object;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        public static FormMain TheMain;

        private DsModel _model;
        private string _dsText;
        private bool _ConvertErr = false;

        public Dictionary<Flo, TabPage> DicUI;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            EventExternal.ProcessSubscribe();
            EventExternal.MSGSubscribe();
            EventExternal.SegSubscribe();

            DicUI = new Dictionary<Flo, TabPage>();

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
                DicUI.Clear();

                splitContainer1.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = false;
                button_OpenFolder.Visible = false;

                this.Size = new Size(1600, 1000);
                HelpLoad();
                ImportPPT();
            }
            catch
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.Error, $"{PathPPT} 불러오기 실패!!");
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.F1) { HelpLoad(); }
            if ((Keys)e.KeyValue == Keys.F5) { ReloadPPT(); }
            if ((Keys)e.KeyValue == Keys.F6) { TestDebug(); }
            if ((Keys)e.KeyValue == Keys.F8) { RefreshGraph(); }
        }
        private void button_copy_Click(object sender, EventArgs e)
        {
            var text = richTextBox_ds.Text;
            RichTextBoxExtensions.SetClipboard(text);
            WriteDebugMsg(DateTime.Now, MSGLevel.Info, $"클립보드복사 성공!! Ctrl+V로 붙여넣기 가능합니다.");

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

        private async void button_TestORG_Click(object sender, EventArgs e)
        {
            button_TestORG.Enabled = false;
            button_TestStart.Enabled = false;
            await SimSeg.TestORG(_model);
            button_TestORG.Enabled = true;
            button_TestStart.Enabled = true;
        }
        private async void button_TestStart_Click(object sender, EventArgs e)
        {
            button_TestORG.Enabled = false;
            button_TestStart.Enabled = false;
            await SimSeg.TestStart(_model);
            button_TestORG.Enabled = true;
            button_TestStart.Enabled = true;
        }

        private void button_comfile_Click(object sender, EventArgs e)
        {
            try
            {
                ExportTextModel(Color.Transparent, _dsText, true);
                var engine = new EngineBuilder(_dsText, $"Cpu_MY").Engine;
            }

            catch (Exception ex)
            {
                MSGError(ex.Message);
            }
        }
    }
}
