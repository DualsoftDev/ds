using DevExpress.Utils.Extensions;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraDiagram.Bars;
using OPC.DSClient.WinForm.UserControl;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm
{
    public partial class MainForm : FluentDesignForm
    {
        private void InitializeEvents()
        {
            var nf = nFrame1;
            accordionControl1.MouseClick += (s, e) =>
            {
                // 클릭된 위치의 요소 찾기
                var hitInfo = accordionControl1.CalcHitInfo(e.Location);

                if (hitInfo.IsInElement)
                {
                    var element = hitInfo.ItemInfo.Element;
                    accordionControl1.Elements.Where(w=>w != element).ForEach(x => x.Expanded = false);       

                    if (ace_Tree == element)         nf.SelectedPage = nPage1;
                    if (ace_Table == element)        nf.SelectedPage = nPage2;
                    if (ace_Sunburst == element)     nf.SelectedPage = nPage3;
                    if (ace_Treemap == element)      nf.SelectedPage = nPage4;
                    if (ace_Heatmap == element)      nf.SelectedPage = nPage5;
                    if (ace_Sankey == element)       nf.SelectedPage = nPage6;
                    if (ace_DataGridFlow == element) nf.SelectedPage = nPage7;
                    if (ace_DataGridIO == element)   nf.SelectedPage = nPage8;
                    if (ace_TextEdit == element)     nf.SelectedPage = nPage9;
                    if (ace_HMI == element)          nf.SelectedPage = nPage10;
                }
            };
        }
        private void InitializeMenu()
        {
            comboBoxEdit_HeatmapScale.Properties.Items.AddRange(new double[] { 0.01, 0.1, 1, 2, 5, 10, 20, 100 });
            comboBoxEdit_HeatmapScale.SelectedItem = 1.0;
            comboBoxEdit_HeatmapScale.Properties.EditValueChanging += (s, e) =>
            {
                 // 입력값이 숫자인지 확인
                if (double.TryParse(e.NewValue?.ToString(), out var parsedValue))
                {
                    e.Cancel = false;
                    HeatmapManager.ScaleUnit = parsedValue;
                }
                else
                {
                    e.Cancel = true;
                }
            };
        
        }
    }
}