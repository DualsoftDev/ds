using log4net.Appender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using DevExpress.XtraEditors;
using static Dsu.PLCConverter.FS.XgiBaseXML;
using System.IO;
using System.Xml.Serialization;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiConfigModule;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiOptionModule;
using Dsu.Common.CS.LSIS.ExtensionMethods;
using Dsu.PLCConverter.FS;
using Dsu.PLCConverter.UI;
using Microsoft.FSharp.Reflection;

namespace MelsecConverter
{
    public partial class FormAddressMapper
        : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
        , IAppender
    {
        private string _DirConfig = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\XGIConvert\\Config";
        private string _PathConfig = Application.StartupPath + @"\\Config\\XgiConfig.xml";
        private string _LastPathConfig = "";
        private XgiConfig _XgiConfig => XgiOption.Config;  

        private void InitConfig()
        {
            if (DialogResult.No == XtraMessageBox.Show("설정을 초기화 하시겠습니까?", "초기화확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                return;
            loadConfig(_PathConfig);
        }

        private void SaveConfig(string path)
        {
            UpdateConfig();
            _XgiConfig.SaveToXml(path);     
        }

        private void loadConfig(string pathConfig)
        {
            XgiOption.Config = XgiConfig.LoadFromXml(pathConfig);
            LoadInitUI();
        }

      
        private void UpdateConfig()
        {
            XgiConfig cnf = _XgiConfig;
            // Bit 영역 업데이트
            cnf.MAreaStart = Convert.ToInt32(textEdit_M.Text);
            cnf.MAreaType = comboBoxEdit_M.Text;

            cnf.LAreaStart = Convert.ToInt32(textEdit_L.Text);
            cnf.LAreaType = comboBoxEdit_L.Text;

            cnf.FAreaStart = Convert.ToInt32(textEdit_F.Text);
            cnf.FAreaType = comboBoxEdit_F.Text;

            cnf.VAreaStart = Convert.ToInt32(textEdit_V.Text);
            cnf.VAreaType = comboBoxEdit_V.Text;

            cnf.SAreaStart = Convert.ToInt32(textEdit_S.Text);
            cnf.SAreaType = comboBoxEdit_S.Text;

            cnf.SBAreaStart = Convert.ToInt32(textEdit_SB.Text);
            cnf.SBAreaType = comboBoxEdit_SB.Text;

            cnf.BAreaStart = Convert.ToInt32(textEdit_B.Text);
            cnf.BAreaType = comboBoxEdit_B.Text;

            cnf.FXAreaStart = Convert.ToInt32(textEdit_FX.Text);
            cnf.FXAreaType = comboBoxEdit_FX.Text;

            cnf.FYAreaStart = Convert.ToInt32(textEdit_FY.Text);
            cnf.FYAreaType = comboBoxEdit_FY.Text;


            cnf.MSmartAreaStart = Convert.ToInt32(textEdit_MSmart.Text);
            cnf.MSmartAreaType = comboBoxEdit_IQ.Text;

            // Word 영역 업데이트
            cnf.DAreaStart = Convert.ToInt32(textEdit_D.Text);
            cnf.DAreaType = comboBoxEdit_D.Text;


            cnf.FDAreaStart = Convert.ToInt32(textEdit_FD.Text);
            cnf.FDAreaType = comboBoxEdit_FD.Text;

            cnf.SWAreaStart = Convert.ToInt32(textEdit_SW.Text);
            cnf.SWAreaType = comboBoxEdit_SW.Text;

            cnf.WAreaStart = Convert.ToInt32(textEdit_W.Text);
            cnf.WAreaType = comboBoxEdit_W.Text;

            cnf.ZRAreaStart = Convert.ToInt32(textEdit_ZR.Text);
            cnf.ZRAreaType = comboBoxEdit_ZR.Text;

            // 기존 타입 유지
            cnf.RAreaStart = Convert.ToInt32(textEdit_R.Text);

            // 타이머 설정 업데이트
            cnf.TimerLowSpeed = Convert.ToInt32(textEdit_TimerN.Text);
            cnf.TimerHighSpeed = Convert.ToInt32(textEdit_TimerH.Text);
        }

        private void LoadInitUI()
        {
            comboBoxEdit_SelXGI.Properties.Items.AddRange(new string[] { "ALL", "%IX", "%QX", "%MX", "%MW", "%RX", "%RW", "%WX", "%WW" });
            comboBoxEdit_SelFail.Properties.Items.AddRange(new string[] { "ALL", "성공", "실패" });

            comboBoxEdit_IQ.Properties.Items.Clear();
            comboBoxEdit_IQ.Properties.Items.Add("IQ");
            comboBoxEdit_IQ.Properties.Items.Add("M");
            comboBoxEdit_IQ.Text = _XgiConfig.MSmartAreaType;
            textEdit_MSmart.Text = $"{_XgiConfig.MSmartAreaStart}";


            for (int i = 0; i < XgiOption.MaxIQLevelB; i++)
                comboBoxEdit_XgiBase.Properties.Items.Add(i);
            comboBoxEdit_XgiBase.SelectedIndex = 0;

            for (int i = 0; i < XgiOption.MaxIQLevelS; i++)
                comboBoxEdit_Slot.Properties.Items.Add(i);
            comboBoxEdit_Slot.SelectedIndex = 0;

            _lstTextXgi_Bit.ForEach(f => f.Text = "0");
            _lstTextXgi_Word.ForEach(f => f.Text = "0");

            textEdit_M.Text = $"{_XgiConfig.MAreaStart}";
            textEdit_B.Text = $"{_XgiConfig.BAreaStart}";
            textEdit_D.Text = $"{_XgiConfig.DAreaStart}";
            textEdit_F.Text = $"{_XgiConfig.FAreaStart}";
            textEdit_FD.Text = $"{_XgiConfig.FDAreaStart}";
            textEdit_FX.Text = $"{_XgiConfig.FXAreaStart}";
            textEdit_FY.Text = $"{_XgiConfig.FYAreaStart}";
            textEdit_L.Text = $"{_XgiConfig.LAreaStart}";
            textEdit_M.Text = $"{_XgiConfig.MAreaStart}";
            textEdit_S.Text = $"{_XgiConfig.SAreaStart}";
            textEdit_SB.Text = $"{_XgiConfig.SBAreaStart}";
            textEdit_SW.Text = $"{_XgiConfig.SWAreaStart}";
            textEdit_V.Text = $"{_XgiConfig.VAreaStart}";
            textEdit_W.Text = $"{_XgiConfig.WAreaStart}";
            textEdit_R.Text = $"{_XgiConfig.RAreaStart}";
            textEdit_ZR.Text = $"{_XgiConfig.ZRAreaStart}";
            textEdit_MSmart.Text = $"{_XgiConfig.MSmartAreaStart}";
            comboBoxEdit_Batch.Text = $"{_XgiConfig.MaxIQLevelM}";

            comboBoxEdit_M.Text = $"{_XgiConfig.MAreaType}";
            comboBoxEdit_B.Text = $"{_XgiConfig.BAreaType}";
            comboBoxEdit_D.Text = $"{_XgiConfig.DAreaType}";
            comboBoxEdit_F.Text = $"{_XgiConfig.FAreaType}";
            comboBoxEdit_FD.Text = $"{_XgiConfig.FDAreaType}";
            comboBoxEdit_FX.Text = $"{_XgiConfig.FXAreaType}";
            comboBoxEdit_FY.Text = $"{_XgiConfig.FYAreaType}";
            comboBoxEdit_L.Text = $"{_XgiConfig.LAreaType}";
            comboBoxEdit_M.Text = $"{_XgiConfig.MAreaType}";
            comboBoxEdit_S.Text = $"{_XgiConfig.SAreaType}";
            comboBoxEdit_SB.Text = $"{_XgiConfig.SBAreaType}";
            comboBoxEdit_SW.Text = $"{_XgiConfig.SWAreaType}";
            comboBoxEdit_V.Text = $"{_XgiConfig.VAreaType}";
            comboBoxEdit_W.Text = $"{_XgiConfig.WAreaType}";
            comboBoxEdit_R.Text = $"{_XgiConfig.RAreaType}";  //고정 R
            comboBoxEdit_ZR.Text = $"{_XgiConfig.ZRAreaType}"; //고정 W

            textEdit_TimerN.Text = $"{_XgiConfig.TimerLowSpeed}";
            textEdit_TimerH.Text = $"{_XgiConfig.TimerHighSpeed}";

            comboBoxEdit_R.SelectedItem = "R";
            comboBoxEdit_ZR.SelectedItem = "W";

            _lstMapping = XgiOption.MappingIO.Select(kv  => Tuple.Create(kv.Key, kv.Value)).ToList();
            _lstUserMapping = XgiOption.MappingSys.Select(kv => Tuple.Create(kv.Key, kv.Value)).ToList();
            gridControl_Mapping.DataSource = _lstMapping;
            gridControl_UserMapping.DataSource = _lstUserMapping;
        }

        private void simpleButton_MappingDelete_Click(object sender, EventArgs e)
        {
            gridView_Mapping.GetSelectedRows().ForEach(f => _lstMapping.RemoveAt(f));
            XgiOption.SetMappingIO(_lstMapping);

            gridControl_Mapping.RefreshDataSource();
        }

        private void simpleButton_UserMappingAdd_Click(object sender, EventArgs e)
        {
            if(_lstUserMapping.Select(s=>s.Item1.ToLower()).Contains(textEdit_UserMelsec.Text.ToLower()))
            {
                MessageBox.Show("이미 등록된 Melsec 주소입니다.");
                return;
            }
            if (textEdit_UserXGI.Text.IsNullOrEmpty())
            {
                MessageBox.Show("XGI 주소를 입력하세요.");
                return;
            }
            if (textEdit_UserMelsec.Text.IsNullOrEmpty())
            {
                MessageBox.Show("Melsec 주소를 입력하세요.");
                return;
            }    

            _lstUserMapping.Add(Tuple.Create(textEdit_UserMelsec.Text, textEdit_UserXGI.Text));
            XgiOption.SetUserMappingIO(_lstUserMapping);
            gridControl_UserMapping.DataSource = _lstUserMapping;
            gridControl_UserMapping.RefreshDataSource();
        }

        private void simpleButton_UserMappingDelete_Click(object sender, EventArgs e)
        {
            gridView_UserMapping.GetSelectedRows().ForEach(f => _lstUserMapping.RemoveAt(f));
            XgiOption.SetUserMappingIO(_lstUserMapping);

            gridControl_UserMapping.RefreshDataSource();
        }

        private void simpleButton_ImportConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "CSV file(*.csv)|*.csv|All files(*.*)|*.*";
                    ofd.Multiselect = true;
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;

                    foreach (var path in ofd.FileNames)
                    {
                        if (path.Contains("Acknowledge XY Assignment"))
                        {
                            var datas = CSVParser.readMxRemoteIO(new List<string> { path });
                            datas.ToList().ForEach(f =>
                            {
                                Logger.Info($"{f}");
                                string RemoteType = f.RemoteType.Contains("Slot ") ? f.RemoteType.Split(' ')[1].TrimEnd(')') : "";
                                string StartX = f.StartX;
                                string StartY = f.StartY;

                                if (RemoteType != "")
                                {
                                    var SlotX = Convert.ToInt32(StartX.Split('-')[0].Replace("Station ", "")) - 1;
                                    var SlotY = Convert.ToInt32(StartY.Split('-')[0].Replace("Station ", "")) - 1;
                                    var worksAdd1 = StartX.Split('>')[1].TrimStart();    //Station  1 -> X0400
                                    var worksAdd2 = StartY.Split('>')[1].TrimStart();    //Station  1 -> Y0400
                                    var XgiAdd1 = $"{("%IX")}{10 + Convert.ToInt32(RemoteType)}.[{SlotX}].[0]";
                                    var XgiAdd2 = $"{("%QX")}{10 + Convert.ToInt32(RemoteType)}.[{SlotY}].[0]";

                                    if (_lstMapping.Where(w => w.Item2 == XgiAdd1).Count() == 0)
                                        AddMapping(worksAdd1, XgiAdd1);
                                    if (_lstMapping.Where(w => w.Item2 == XgiAdd2).Count() == 0)
                                        AddMapping(worksAdd2, XgiAdd2);
                                }
                            });
                        }
                        else
                        if (path.Contains("IO Assignment Setting"))
                        {
                            var datas = CSVParser.readMxIO(new List<string> { path });
                            datas.ToList().ForEach(f =>
                            {
                                Logger.Info($"{f}");
                                string Slot = f.Slot.Split('(')[0];
                                string Type = f.Type;
                                int StartXY = f.StartXY;
                                int Points = f.Points;
                                if (StartXY == -1)
                                    StartXY = Convert.ToInt32(Slot) * _XgiConfig.MaxIQLevelM;

                                if (Type == "Input" || Type == "Output")
                                {
                                    var worksAdd = $"{(Type == "Input" ? "X" : "Y")}{StartXY.ToString("X4")}";
                                    var XgiAdd = $"{(Type == "Input" ? "%IX" : "%QX")}{0}.[{Slot}].[0]";
                                    AddMapping(worksAdd, XgiAdd);
                                }
                                else if (Type == "I/O Mix")
                                {
                                    var worksAdd1 = $"{"X"}{StartXY.ToString("X4")}";
                                    var worksAdd2 = $"{"Y"}{(StartXY + 16).ToString("X4")}";
                                    var XgiAdd1 = $"{("%IX")}{0}.[{Slot}].[0]";
                                    var XgiAdd2 = $"{("%QX")}{0}.[{Slot}].[16]";
                                    AddMapping(worksAdd1, XgiAdd1);
                                    AddMapping(worksAdd2, XgiAdd2);
                                }

                            });
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                MsgBox.Error("에러", $"로딩에 실패하였습니다.\r\n{ex.Message}");
            }
        }
        private void simpleButton_MappingAdd_Click(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textEdit_UserMelsec.Text, @"[0-9A-Fa-f]+$"))
            {
                var worksAdd = $"{(radioButton_X.Checked ? "X" : "Y")}{textEdit_UserMelsec.Text.ToUpper()}";
                var XgiAdd = $"{(radioButton_X.Checked ? "%IX" : "%QX")}" +
                    $"{comboBoxEdit_XgiBase.Text}." +
                    $"[{comboBoxEdit_Slot.Text}]." +
                    $"[0]";

                AddMapping(worksAdd, XgiAdd);
            }
            else
            {
                MessageBox.Show($"[{textEdit_UserMelsec.Text}] XY주소로 hexa 값을 입력하세요");
            }
        }


        bool AddMapping(string worksAdd, string XgiAdd)
        {
            if (!worksAdd.EndsWith("0"))
            {
                Logger.Warn($"[{worksAdd}] XY주소 시작비트는 0이여야 합니다.");
                return false;
            }
            if (_lstMapping.Where(w => w.Item1 == worksAdd).Count() > 0)
            {
                Logger.Warn($"[{worksAdd}] XY주소로 이미 등록했습니다.");
                return false;
            }

            if (_lstMapping.Where(w => w.Item2 == XgiAdd).Count() > 0)
            {
                Logger.Warn($"[{XgiAdd}] XGI주소로 이미 등록했습니다.");
                return false;
            }

            _lstMapping.Add(Tuple.Create(worksAdd, XgiAdd));
            XgiOption.SetMappingIO(_lstMapping);
            gridControl_Mapping.DataSource = _lstMapping;
            gridControl_Mapping.RefreshDataSource();

            return true;
        }

        private void InitUIControl()
        {
            comboBox_CPUs.Properties.Items.AddRange(XgiOption.CPUs);
            comboBox_CPUs.SelectedIndex = comboBox_CPUs.Properties.Items.Count - 1;

            _lstComboXgi.Add(comboBoxEdit_M);
            _lstComboXgi.Add(comboBoxEdit_L);
            _lstComboXgi.Add(comboBoxEdit_F);
            _lstComboXgi.Add(comboBoxEdit_V);
            _lstComboXgi.Add(comboBoxEdit_S);
            _lstComboXgi.Add(comboBoxEdit_SB);
            _lstComboXgi.Add(comboBoxEdit_B);
            _lstComboXgi.Add(comboBoxEdit_FX);
            _lstComboXgi.Add(comboBoxEdit_FY);
            _lstComboXgi.Add(comboBoxEdit_D);
            _lstComboXgi.Add(comboBoxEdit_W);
            _lstComboXgi.Add(comboBoxEdit_SW);
            _lstComboXgi.Add(comboBoxEdit_FD);


            //_lstComboXgi.Add(comboBoxEdit_ZR);

            _lstTextXgi_Bit.Add(textEdit_M);
            _lstTextXgi_Bit.Add(textEdit_L);
            _lstTextXgi_Bit.Add(textEdit_F);
            _lstTextXgi_Bit.Add(textEdit_V);
            _lstTextXgi_Bit.Add(textEdit_S);
            _lstTextXgi_Bit.Add(textEdit_SB);
            _lstTextXgi_Bit.Add(textEdit_B);
            _lstTextXgi_Bit.Add(textEdit_FX);
            _lstTextXgi_Bit.Add(textEdit_FY);
            _lstTextXgi_Bit.Add(textEdit_MSmart);

            _lstTextXgi_Word.Add(textEdit_D);
            _lstTextXgi_Word.Add(textEdit_W);
            _lstTextXgi_Word.Add(textEdit_SW);
            _lstTextXgi_Word.Add(textEdit_FD);
            _lstTextXgi_Word.Add(textEdit_R);
            _lstTextXgi_Word.Add(textEdit_ZR);

            SettingAddressComboBox(radioButton_Tag.Checked);
        }

        private void SettingAddressComboBox(bool globalType)
        {
            List<string> XgiAddressTypes = FSharpType.GetUnionCases(typeof(XGI), null)
                                                .Where(w => (w.Name != "S" && w.Name != "I" && w.Name != "Q"))
                                                .Where(w => globalType || w.Name != "A")
                                                .Select(s => s.Name).ToList(); // 기타변수(S) 제외


            _lstComboXgi.ForEach(f => f.Properties.Items.Clear());
            _lstComboXgi.ForEach(f => f.Properties.Items.AddRange(XgiAddressTypes));
            _lstComboXgi.ForEach(f => f.SelectedItem = "M");
        }

    }
}