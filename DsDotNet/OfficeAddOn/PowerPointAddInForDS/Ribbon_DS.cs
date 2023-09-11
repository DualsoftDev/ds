using Engine.Core;
using Engine.Import.Office;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Import.Office.ImportPPTModule;

namespace PowerPointAddInForDS
{
    public partial class Ribbon_DS
    {
        Presentation _ppt => Globals.ThisAddIn.Application.ActivePresentation;

        private void ShowTextDS()
        {
            try
            {
                var pptResults = ImportPPT.GetModel(new string[] { _ppt.FullName });
                var sys = pptResults.Systems[0];
                var txt = SystemToDsExt.ToDsText(sys, false);

                FormDocText txtDlg = new FormDocText();
                txtDlg.TextEdit.AppendText(txt);
                txtDlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"정보확인 필요\r\n{ex.Message}");
            }
        }
        private void ShowGraphDS()
        {
            try
            {
                string selectTitle = ""; // Replace with the title you're looking for
                foreach (Slide slide in _ppt.Slides)
                {
                    var mm = slide.Master;
                    if (_selectSlideIndex == slide.SlideIndex
                        && slide.Shapes.HasTitle == MsoTriState.msoTrue
                        && slide.Layout != PpSlideLayout.ppLayoutSectionHeader
                        && slide.Layout != PpSlideLayout.ppLayoutTitle
                        && slide.Layout != PpSlideLayout.ppLayoutTitleOnly
                        && slide.Layout != PpSlideLayout.ppLayoutVerticalTitleAndText
                        && slide.Layout != PpSlideLayout.ppLayoutVerticalTitleAndTextOverChart
                        )
                    {
                        selectTitle = slide.Shapes.Title.TextFrame.TextRange.Text;
                        break;
                    }
                }

                if (selectTitle != "")
                {
                    var ret = ImportPPT.GetLoadingAllSystem(new string[] { _ppt.FullName });
                    var model = ret.Item1;
                    var pptResults = ret.Item2;

                    var activeSys = pptResults.First(f => f.IsActive).System;
                    IEnumerable<ViewModule.ViewNode> viewSet =
                        pptResults.First(f => f.System == activeSys).Views;

                    FormDocView dlg = new FormDocView();
                    var nodeFlows = viewSet.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                   .Where(w => w.UsedViewNodes.Any())
                                   .First(w => w.Flow.Value.Name == selectTitle);


                    dlg.UcView.SetGraph(nodeFlows, nodeFlows.Flow.Value, false);
                    dlg.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"선택된 페이지 (page:{_selectSlideIndex})에 타이틀 이름이 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"정보확인 필요\r\n{ex.Message}");
            }
        }
        private void CheckDS()
        {
            try
            {
                var pptResults = ImportPPT.GetModel(new string[] { _ppt.FullName });
                var sys = pptResults.Systems[0];
                var txt = SystemToDsExt.ToDsText(sys, false);
                MessageBox.Show($"Dualsoft 언어체크 성공!!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"언어체크 실패, 정보확인 필요\r\n{ex.Message}");
            }
        }
        private void button_showDSDiagram_Click(object sender, RibbonControlEventArgs e)
        {
            _ppt.Save(); //강제 세이브 (이전 파일이 유지되서)
            ShowGraphDS();
        }

        private void button_showDSText_Click(object sender, RibbonControlEventArgs e)
        {
            _ppt.Save(); //강제 세이브 (이전 파일이 유지되서)
            ShowTextDS();
        }

        private void button_checkDS_Click(object sender, RibbonControlEventArgs e)
        {
            _ppt.Save(); //강제 세이브 (이전 파일이 유지되서)
            CheckDS();
        }
        int _selectSlideIndex = 0;

        private void Ribbon_DS_Load(object sender, RibbonUIEventArgs e)
        {
            Globals.ThisAddIn.Application.SlideSelectionChanged += (ss) => _selectSlideIndex = ss.SlideIndex;
        }
    }
}
