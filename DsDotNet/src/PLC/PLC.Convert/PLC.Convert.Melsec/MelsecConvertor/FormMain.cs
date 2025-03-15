using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Dsu.Common.CS.LSIS.ExtensionMethods;
using Dsu.PLCConverter.FS;
using Dsu.PLCConverter.UI;
using log4net.Appender;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Dsu.PLCConverter.FS.XgiBaseXML;
using static Dsu.PLCConverter.FS.XgiSymbol;
using System.Xml.Serialization;
using System.Security;
using Microsoft.FSharp.Reflection;
using DevExpress.XtraBars.Navigation;
using static Dsu.PLCConverter.FS.CSVTypes;
using Microsoft.Win32;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiConfigModule;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiOptionModule;
using System.Diagnostics;

namespace MelsecConverter
{
    public partial class FormAddressMapper
        : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
        , IAppender
    {
        private List<SymbolInfo> _lstSymbolXGI = new List<SymbolInfo>();
        private List<SymbolInfo> _lstPreViewSymbolXGI = new List<SymbolInfo>();
        private List<ComboBoxEdit> _lstComboXgi = new List<ComboBoxEdit>();
        private List<TextEdit> _lstTextXgi_Bit = new List<TextEdit>();
        private List<TextEdit> _lstTextXgi_Word = new List<TextEdit>();
        private List<Tuple<string, string>> _lstMapping = new List<Tuple<string, string>>();
        private List<Tuple<string, string>> _lstUserMapping = new List<Tuple<string, string>>();


        public FormAddressMapper()
        {
            InitializeComponent();
        }

        private void FormAddressMapper_Load(object sender, EventArgs args)
        {
            gridControl_Result.DataSource = _logs;
            gridView_Result.RowCountChanged += (s, e) =>
            {
                label_LogCount.Text = $"{gridView_Result.RowCount}";    
            };  

            Logger.Info("Application launched.");

            var asmName = System.Reflection.Assembly.GetEntryAssembly().GetName();
            this.Text += $"  Ver {asmName.Version.Major}.{asmName.Version.Minor}.{asmName.Version.Build}";

            KeyPreview = true;
            this.KeyDown += FormAddressMapper_KeyDownAsync; ;


#if !DEBUG
            Thread.Sleep(500);
            DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
#endif

            XgiOption.PathCommandMapping = Application.StartupPath + @"\\Config\\CommandMapping.csv";
            XgiOption.PathFBList = Application.StartupPath + @"\\Config\\XG5000_IEC_FUNC.txt";

            gridView_Mapping.BestFitColumns();
      

            loadConfig(_PathConfig);
            InitUIControl();

            CSVParser.RowProcessed.AddHandler((ss, rowCount) =>
            {
                UpdateProcessDisplay($"Convert: {rowCount}/{totalRowCount}", 0, "");
            });

            CSVParser.TotalLines.AddHandler((ss, total) =>
            {
                totalRowCount = total;
            });

            //navigationFrame.SelectedPageIndex = 1;
        }

      
        int totalRowCount = 0;
        private async void FormAddressMapper_KeyDownAsync(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4 && sender != null)
            {
                OpenPouOrCommentCSV();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.F5 && sender != null)
            {
                _PouCommentPaths = LoadPathsFromRegistry();
                await ConvertXGI();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Escape && sender != null)
            {
                Macro.StopExport();
                e.Handled = true;
            }
        }

      

        private void FormAddressMapper_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult.No == XtraMessageBox.Show("종료하시겠습니까?", "종료확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                e.Cancel = true;
        }

        private string _LastOutputPath = "";
        private List<string> _PouCommentPaths = new List<string>();

        private string _DirOutputPath =>
                $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}" +
                $"\\XGIConvert\\" +
                $"{(new DirectoryInfo(_LastOutputPath)).Parent.Name}";

        private void UpdateProcessDisplay(string Cation, int percent, string text)
        {
            this.Do(() =>
            {
                barStaticItem_Process.Caption = Cation;
                barEditItem_Process.EditValue = percent;
                if (!text.IsNullOrEmpty())
                {
                    text.Split('\n').Iter(f => ucPanelLog1.Items.Add($"{(ucPanelLog1.Items.Count).ToString("00000")}: {f}"));
                }
                // ucPanelLog_Convert.SelectedIndex = ucPanelLog_Convert.Items.Count - 1;
            });
        }

        private async void simpleButton_SetAddress_Click(object sender, EventArgs e)
        {
            await PreViewAddress();
        }

        private async Task PreViewAddress()
        {
            try
            {
                checkEdit_BitShow.Checked = false;
                UpdateConfig();

                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "CSV file(*.csv)|*.csv|All files(*.*)|*.*";
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;
                    var path = Path.GetDirectoryName(ofd.FileName);
                    var csvs = Directory.GetFiles(path).Where(f => f.ToLower().EndsWith("csv"))
                        .Where(d => !(d.Contains("Acknowledge XY Assignment") || d.Contains("IO Assignment Setting")));


                    //ucPanelLog_Convert.Items.Clear();
                    int FileCount = 0;

                    using (var waitor = new SplashScreenWaitor("로딩", "파일을 불러오는 중 입니다."))
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                List<POUParseResult> pous = new List<POUParseResult>();
                                Dictionary<string, string> commentDic = new Dictionary<string, string>();
                                Dictionary<string, string> globalLabelDic = new Dictionary<string, string>();
                                csvs.Iter(i =>
                                {
                                    int percent = Convert.ToInt32((double)++FileCount / (csvs.Count() + 1) * 100);
                                    UpdateProcessDisplay($"Convet : {i}", percent, "");
                                    (POUParseResult[] p, Dictionary<string, string> c, var g) = CSVParser.parseCSVs(new string[] { i });
                                    pous.AddRange(p);
                                    if (commentDic.Count() == 0)
                                        commentDic = c;
                                    else
                                        c.ForEach(f => commentDic.Add(f.Key, f.Value));
                                    if (globalLabelDic.Count() == 0)
                                        globalLabelDic = g;
                                    else
                                        g.ForEach(f => globalLabelDic.Add(f.Key, f.Value));
                                });
                                var allSymbols = XgiFile.getAllSymbols(pous, commentDic, globalLabelDic);
                                (this).Do(() =>
                                {
                                    _lstPreViewSymbolXGI = GetCheckedSymbols(allSymbols.Item1.ToList());

                                    gridControl_XGIAddress.DataSource = _lstSymbolXGI;
                                    comboBoxEdit_SelXGI.SelectedIndex = 0;
                                    comboBoxEdit_SelFail.SelectedIndex = 0;

                                    RefreshXgiSymbol();
                                });

                                UpdateProcessDisplay("Ready", 0, "");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"{ex}");
                                MsgBox.Error("에러", $"로딩에 실패하였습니다.\r\n{ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                MsgBox.Error("에러", $"로딩에 실패하였습니다.\r\n{ex.Message}");
            }
        }

        private List<SymbolInfo> GetCheckedSymbols(List<SymbolInfo> allSymbols)
        {
            _lstSymbolXGI = allSymbols;
            Dictionary<string, int> dic = new Dictionary<string, int>();
            _lstSymbolXGI.ForEach(s =>
            {
                if (s.Address != "")
                {
                    if (!dic.ContainsKey(s.Address))
                        dic.Add(s.Address, 1);
                    else
                        dic[s.Address] = dic[s.Address] + 1;
                }
            });

            _lstSymbolXGI.AsParallel().ForEach(f => { if (f.Address != "") f.SameAddress = dic[f.Address]; });
            return _lstSymbolXGI;
        }


        private void comboBoxEdit_SelXGI_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshXgiSymbol();
        }

        private void RefreshXgiSymbol()
        {
            IEnumerable<SymbolInfo> selectSRC;

            if (comboBoxEdit_SelXGI.SelectedIndex <= 0)
                selectSRC = _lstPreViewSymbolXGI;
            else
                selectSRC = _lstPreViewSymbolXGI.Where(w => w.Address.Contains(comboBoxEdit_SelXGI.SelectedItem.ToString()));

            if (comboBoxEdit_SelFail.SelectedIndex > 0)
            {
                var filterText = comboBoxEdit_SelFail.SelectedItem.ToString() == "실패" ? "NOT_XGI" : "%";
                selectSRC = selectSRC.Where(w => w.Address.Contains(filterText));
            }


            gridControl_XGIAddress.DataSource = selectSRC.OrderBy(o => o.Index);
            gridView_XGIAddress.BestFitColumns();

            labelControl_Count.Text = $"{selectSRC.Count()}";
        }

   

        private void radioButton_Tag_CheckedChanged(object sender, EventArgs e)
        {
            SettingAddressComboBox(radioButton_Tag.Checked);
        }

        private void comboBox_CPUs_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<Tuple<string, string, string>> lstMaxCpu = new List<Tuple<string, string, string>>();

            XgiOption.SelectCPU = comboBox_CPUs.SelectedIndex;

            lstMaxCpu.Add(Tuple.Create("I", $"{XgiOption.I_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.I_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("Q", $"{XgiOption.Q_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.Q_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("M", $"{XgiOption.M_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.M_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("L", $"{XgiOption.L_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.L_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("N", $"{XgiOption.N_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.N_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("K", $"{XgiOption.K_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.K_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("U", $"{XgiOption.U_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.U_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("R", $"{XgiOption.R_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.R_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("A", $"{XgiOption.A_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.A_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("W", $"{XgiOption.W_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.W_CPUMax[XgiOption.SelectCPU] * 16})"));
            lstMaxCpu.Add(Tuple.Create("F", $"{XgiOption.F_CPUMax[XgiOption.SelectCPU]}", $"({XgiOption.F_CPUMax[XgiOption.SelectCPU] * 16})"));

            gridControl_CpuMax.DataSource = lstMaxCpu;
            gridView_CpuMax.BestFitColumns();
        }
        private void comboBoxEdit_IQ_SelectedIndexChanged(object sender, EventArgs e)
        {
            textEdit_MSmart.Enabled = comboBoxEdit_IQ.Text != "IQ";
            groupBoxMappingIO.Enabled = comboBoxEdit_IQ.Text == "IQ"; 
        }
        private void comboBoxEdit_Batch_SelectedIndexChanged(object sender, EventArgs e)
        {
            XgiOption.MaxIQLevelM = Convert.ToInt32(comboBoxEdit_Batch.Text);
        }

        private void checkEdit_BitShow_Properties_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit_BitShow.Checked)
            {
                _lstTextXgi_Bit.ForEach(f => f.Enabled = false);
                _lstTextXgi_Bit.ForEach(f => f.Text = (Convert.ToInt64(f.Text) * 16).ToString());

                textEdit_MSmart.Enabled = false;
                textEdit_MSmart.Text = (Convert.ToInt64(textEdit_MSmart.Text) * 16).ToString();
            }
            else
            {
                _lstTextXgi_Bit.ForEach(f => f.Enabled = true);
                _lstTextXgi_Bit.ForEach(f => f.Text = (Convert.ToInt64(f.Text) / 16).ToString());

                textEdit_MSmart.Enabled = true;
                textEdit_MSmart.Text = (Convert.ToInt64(textEdit_MSmart.Text) / 16).ToString();
            }
        }



     

        private void gridView_XGIAddress_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.RowHandle < 0) return;

            SymbolInfo showSym = view.GetRow(e.RowHandle) as SymbolInfo;
            if (showSym.SameAddress > 1)
                e.Appearance.BackColor = Color.FromArgb(60, Color.Salmon);
        }



        private void acc_InitConfig_Click(object sender, EventArgs e)
        {
            checkEdit_BitShow.Checked = false;
            InitConfig();
        }

        private void acc_OpenConfig_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {

                ofd.InitialDirectory = _DirConfig;
                ofd.Filter = "xml file(*.xml)|*.xml";
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                _ = XtraMessageBox.Show($@"{ofd.FileName} 설정 가져오기 완료", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
           
                loadConfig(ofd.FileName);
                _LastPathConfig = ofd.FileName;
                navigationFrame.SelectedPageIndex = 1;
            }
        }

        private void acc_SaveConfig_Click(object sender, EventArgs e)
        {
            // _LastPathConfig가 비어 있거나 null인 경우 처리
            if (string.IsNullOrWhiteSpace(_LastPathConfig))
            {
                SaveAsConfig();
                return;
            }
            void SaveAsConfig()
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "xml|*.xml",
                    InitialDirectory = _DirConfig,
                    Title = "Save a config File"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    SaveConfig(saveFileDialog.FileName);
                }
            }


            var returnDlg = XtraMessageBox.Show(
                    $"{_LastPathConfig} 저장하시겠습니까? [no:다른이름저장]",
                    "저장확인",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information
                );

            switch (returnDlg)
            {
                case DialogResult.No:
                    SaveAsConfig();
                    break;

                case DialogResult.Cancel:
                    return;

                case DialogResult.Yes:
                    SaveConfig(_LastPathConfig);
                    break;
            }

            checkEdit_BitShow.Checked = false;
        }

        private async void acc_ConvertXGI_Click(object sender, EventArgs e)
        {
            await ConvertXGI();
        }

        private void acc_InitView_Click(object sender, EventArgs e) => navigationFrame.SelectedPageIndex = 0;
        private void acc_AddressSetting_Click(object sender, EventArgs e)
        {
            navigationFrame.SelectedPageIndex = 1;
        }
        private void acc_AddressMapping_Click(object sender, EventArgs e)
        {
            navigationFrame.SelectedPageIndex = 3;
        }

        private async void acc_AddressPreview_Click(object sender, EventArgs e)
        {
            navigationFrame.SelectedPageIndex = 2;
            await PreViewAddress();

        }
        private void simpleButton_ImportPOU_Click(object sender, EventArgs e)
        {
            OpenPouOrCommentCSV();
        }
        private  void acc_PouMacroWorks3_Click(object sender, EventArgs e)
        {
            if (MsgBox.Ask("Works3 CSV 자동추출", $"POU 추출 하려면 \r\n" +
                $"확인을 누른후 열려있는 Works3를 5초 이내 활성화 하면 됩니다.") == DialogResult.Yes)
            {
                Macro.Delay = Convert.ToInt32(spinEdit_CSVDelay.EditValue);
                var res = Macro.StartExportGx3();
                if (res == true)
                    MsgBox.Info("일괄 Export완료");
                else
                    MsgBox.Info("작업이 취소되었습니다.");
            }

        }
        private  void acc_PouMacroWorks2_Click(object sender, EventArgs e)
        {
            if (MsgBox.Ask($"Works2 CSV 자동추출 반복 취소는 ESC", $"POU 추출 하려면 \r\n" +
               $"확인을 누른후 열려있는 Works2를 5초 이내 활성화 하면 됩니다.") == DialogResult.Yes)
            {
                Macro.Delay = Convert.ToInt32(spinEdit_CSVDelay.EditValue);
                var res = Macro.StartExportGx2(Int32.MaxValue);
                if (res == true)
                    MsgBox.Info("일괄 Export완료");
                else
                    MsgBox.Info("작업이 취소되었습니다.");
            }
        }
        private void acc_OpenResultFolder_Click(object sender, EventArgs e)
        {
            if (!_LastOutputPath.IsNullOrEmpty())
                System.Diagnostics.Process.Start("explorer.exe", _DirOutputPath);
            else
                MsgBox.Info("변환된 파일이 없습니다.");
        }
        private void acc_PouMacroStop_Click(object sender, EventArgs e)
        {
            Macro.StopExport(); 
        }

        private void acc_OpenManual_Click(object sender, EventArgs e)
        {
            string filePath = @"manual.pptx"; // 열고자 하는 PowerPoint 파일 경로

            try
            {
                // PowerPoint 파일 실행
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // 기본 애플리케이션으로 실행
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일을 여는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}