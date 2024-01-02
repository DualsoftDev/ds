using Diagram.View.MSAGL;

using Dual.Common.Core;
using Dual.Common.Winform;

using Engine.Core;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Cpu.RunTime;
using static Engine.Import.Office.ViewModule;

namespace DsWebApp.Simulatior
{
    public partial class FormDocViewSim : Form
    {


        private readonly IDisposable _DisposableTagDS;
        private readonly DsCPU _DsCPU;
        private readonly DsSystem _DsSys;

        public FormDocViewSim(DsSystem sys, DsCPU cpu)
        {
            InitializeComponent();
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

            _DsSys = sys;
            _DsCPU = cpu;

            _DisposableTagDS?.Dispose();
            _DisposableTagDS = TagDSSubject
                .Where(w => w.GetSystem() == sys)
                .Subscribe(evt =>
            {
                if (evt.IsEventVertex)
                {
                    EventVertex t = evt as EventVertex;
                    if ((evt.IsStatusTag() && (bool)t.Tag.BoxedValue)
                        || evt.IsVertexErrTag())
                    {
                        ViewUtil.VertexChangeSubject.OnNext(t);
                        label_log.Do(() => label_log.Text = t.GetTagToText());
                    }
                }
            });

            FormClosing += (s, e) =>
            {
                _DsCPU?.Stop();
                _DsCPU?.Dispose();
            };

            button_Pause.Enabled = false;
            label_log.Text = string.Empty;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Graphics g = CreateGraphics();
            try
            {
                float dpiX = g.DpiX;
                float scale = dpiX / 96; // 96은 표준 DPI 값입니다.

                // 폼 내의 모든 컨트롤 스케일링
                ScaleControl(this, scale);
            }
            finally
            {
                g.Dispose();
            }
        }
        private void ScaleControl(Control control, float scale)
        {
            // 위치와 크기 모두 조정
            control.Location = new Point((int)(control.Location.X * scale), (int)(control.Location.Y * scale));
            control.Size = new Size((int)(control.Size.Width * scale), (int)(control.Size.Height * scale));

            // 폰트 크기 조정
            control.Font = new Font(control.Font.FontFamily, control.Font.Size * scale, control.Font.Style);

            //// 컨트롤이 다른 컨트롤을 포함하는 경우(예: 패널, 그룹 박스 등)에는 그 내부의 컨트롤도 스케일링
            //foreach (Control child in control.Controls)
            //{
            //    ScaleControl(child, scale);
            //}
        }

        public void RunCpu()
        {
            _ = Task.Run(() =>
            {
                _DsCPU.Run();
            });
        }
        public void StopCpu()
        {
            _ = Task.Run(() =>
            {
                _DsCPU.Stop();
            });
        }
        public async Task StepCpuAsync()
        {
            _ = await _DsCPU.StepByStatusAsync(_DsSys);
        }

        private void SetUcView(UcView ucView)
        {
            ucView.Dock = System.Windows.Forms.DockStyle.Fill;
            ucView.Location = new System.Drawing.Point(0, 0);
            ucView.Name = ucView.Name;
            ucView.Size = new System.Drawing.Size(694, 570);
            ucView.TabIndex = 0;

            TabPage tabPage = new();
            tabPage.Controls.Add(ucView);
            tabPage.Location = new System.Drawing.Point(4, 22);
            tabPage.Padding = new System.Windows.Forms.Padding(3);
            tabPage.Size = new System.Drawing.Size(686, 544);
            tabPage.TabIndex = 0;
            tabPage.Text = ucView.Flow.Name;
            tabPage.Tag = ucView.Flow;
            tabPage.UseVisualStyleBackColor = true;
            tabControl.Controls.Add(tabPage);
            tabControl.SelectedTab = tabPage;
        }

        internal void ShowGraph(DsSystem activeSys, Dictionary<ViewNode, UcView> dicView, string selectPageName)
        {
            dicView/*Where(dic => dic.Key.UsedViewVertexNodes().Any())*/
                   .Where(dic => dic.Key.Flow.Value.System == activeSys)
                   .Iter(dic =>
            {
                SetUcView(dic.Value);
            });



            TabPage tagPage = tabControl.TabPages
                .Cast<TabPage>()
                .FirstOrDefault(w => ((Flow)w.Tag).Name == selectPageName);

            if (tagPage != null)
            {
                tabControl.SelectedTab = tagPage;
            }
            else
            {
                if (tabControl.TabPages.IsNullOrEmpty())
                {
                    label_log.Text = $"{activeSys.Name} system is empty!!";
                }
                else
                {
                    label_log.Text = $"{activeSys.Name} system is loading!!";
                    tabControl.SelectedTab = tabControl.TabPages[0];
                }
            }
            if (tabControl.SelectedTab != null)
            {
                SelectPageChanged();
            }
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (comboBox_Real.SelectedItem is Real r)
            {
                VertexManager vv = r.TagManager as VertexManager;
                vv.SF.Value = true;
            }
        }

        private void button_Reset_Click(object sender, EventArgs e)
        {
            if (comboBox_Real.SelectedItem is Real r)
            {
                VertexManager vv = r.TagManager as VertexManager;
                vv.RF.Value = true;
            }
        }
        private async void button_Step_Click(object sender, EventArgs e)
        {
            button_Play.Enabled = true;
            button_Pause.Enabled = false;
            label_log.Text = "Step";

            await StepCpuAsync();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectPageChanged();
        }
        private void SelectPageChanged()
        {
            if (tabControl.SelectedTab.Tag is Flow f)
            {
                Real[] reals = f.Graph.Vertices.OfType<Real>().OrderBy(s => s.Name).ToArray();
                if (reals.Any())
                {
                    comboBox_Real.Items.Clear();
                    comboBox_Real.Items.AddRange(reals);
                    comboBox_Real.DisplayMember = "Name";
                    comboBox_Real.SelectedIndex = 0;
                }
            }
        }
        private void button_Play_Click(object sender, EventArgs e)
        {
            RunCpu();

            button_Play.Enabled = false;
            button_Pause.Enabled = true;
            label_log.Text = "Play";
        }
        private void button_Pause_Click(object sender, EventArgs e)
        {
            StopCpu();

            button_Play.Enabled = true;
            button_Pause.Enabled = false;
            label_log.Text = "Pause";
        }
        
    }
}