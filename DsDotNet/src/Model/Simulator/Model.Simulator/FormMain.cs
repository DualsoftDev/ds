using Engine;
using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using Engine.Parser;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreClass;
using static Engine.Core.CoreFlow;
using static Engine.Core.DsType;
using Task = System.Threading.Tasks.Task;

namespace Model.Simulator
{
    public partial class FormMain : Form
    {
        public static FormMain TheMain;

        private CoreStruct.Model _Model = new CoreStruct.Model();
        private string _dsText;

        public Dictionary<Flow, TabPage> DicUI;
        public string _dsTextPath;
        public bool Busy = false;

        public FormMain()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            TheMain = this;


            richTextBox_Debug.AppendText($"{DateTime.Now} : Application DS simulator is starting");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            EventExternal.ProcessSubscribe();
            EventExternal.MSGSubscribe();

            DicUI = new Dictionary<Flow, TabPage>();
            MSGInfo("*.ds 를 드랍하면 시작됩니다");
            //this.Text = UtilFile.GetVersion();


        }
        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);
                if (extension == ".ds")
                {
                    LoadText(file);
                    break; //단일 파일만
                }
            }
        }
        public void UpdateProgressBar(int percent)
        {
            progressBar1.Do(() => progressBar1.Value = percent);
        }
        private async void LoadText(string path)
        {
            try
            {
                _dsTextPath = path;

                richTextBox_ds.Clear();
                DicUI.Clear();

                _dsText = File.ReadAllText(path);
                await Task.Run(() => { ExportTextModel( _dsText); });

                ProcessEvent.DoWork(0);

            }
            catch
            {
                MSGError($"{_dsTextPath} 불러오기 실패!!");
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keys)e.KeyValue == Keys.F1) { HelpLoad(); }
            if ((Keys)e.KeyValue == Keys.F5) { ReLoadText(); }
            if ((Keys)e.KeyValue == Keys.F6) { TestDebug(); }
            if ((Keys)e.KeyValue == Keys.F8) { RefreshGraph(); }
        }
        private void button_ClearLog_Click(object sender, EventArgs e)
        {
            richTextBox_Debug.Clear();
            richTextBox_Debug.AppendText($"{DateTime.Now} : Log Clear");

            try
            {
                var modelText = Tester.GetTextDiamond();
                //var eb = new EngineBuilder(modelText, ParserOptions.Create4Simulation("Cpu"));
            }

            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.Error, ex.Message);
            }


        }
        private void button_Compile_Click(object sender, EventArgs e)
        {
            try
            {
                button_TestORG.Enabled = true;
                button_TestStart.Enabled = true;


                RefreshText();
                ExportTextModel(_dsText, true);

                if (_dsText == "")
                {
                    MSGWarn("모델을 Text 영역에 가져오세요");
                    return;
                }
                else
                {
                    xtraTabControl_My.TabPages.Clear();
                    DicUI.Clear();

                    var helper = ModelParser.ParseFromString2(_dsText, ParserOptions.Create4Simulation());
                    _Model = helper.Model;

                    _Model.Systems.ForEach(f =>
                    {
                        CreateNewTabViewer(f);
                    });
                }

                ProcessEvent.DoWork(0);

            }

            catch (Exception ex)
            {
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
            _dsText = _dsText.TrimEnd('\n');
            richTextBox_ds.Text = _dsText;
        }

        private void button_HideLine_Click(object sender, EventArgs e)
        {
            RefreshText();

        }

    }
}
