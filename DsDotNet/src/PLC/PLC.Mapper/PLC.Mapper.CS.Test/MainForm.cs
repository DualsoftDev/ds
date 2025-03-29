using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PLC.Mapper.FS;
using static PLC.Mapper.FS.MapperDataModule;

namespace PLC.Mapper.CS.Test
{
    public partial class MainForm : Form
    {
        private const int LineHeight = 18;
        private const int CanvasMargin = 10;
        private const int CanvasWidth = 1600;

        private readonly Font DrawFont = new("Consolas", 14, FontStyle.Bold);
        private readonly Dictionary<char, Color> charColors = new();

        public MainForm()
        {
            InitializeComponent();
            this.Shown += (s, e) => Generate();
        }

        private void Generate()
        {
            var names = LoadNameList();
            BuildCharColorMap(names);
            var list = MappingDeviceModule.extractGroupDeviceApis(names, 5);
            var image = RenderGroupedImage(list);
            pictureBox1.Image = image;
        }


        private List<string> LoadNameList()
        {
            return new List<string>
            {
                "S204_SOL_1ST_C_TYPE_LATCH1_ADV",
                "S204_SOL_1ST_C_TYPE_LATCH1_RET",
                "S204_SOL_1ST_C_TYPE_LATCH2_ADV",
                "S204_SOL_1ST_C_TYPE_LATCH2_RET",
                "S204_SOL_1ST_C_TYPE_LATCH3_ADV",
                "S204_SOL_1ST_C_TYPE_LATCH3_RET",
                "S204_SOL_1ST_CLAMP1_RET",
                "S204_SOL_1ST_CLAMP2_ADV",
                "S204_SOL_1ST_CLAMP2_RET",
                "S204_SOL_1ST_CLAMP3_ADV",
                "S204_SOL_1ST_CLAMP3_RET",
                "S204_SOL_1ST_CLAMP4_ADV",
                "S204_SOL_1ST_CLAMP4_RET",
                "S204_SOL_1ST_CLAMP5_ADV",
                "S204_SOL_1ST_CLAMP5_RET",
                "S204_SOL_1ST_CLAMP6_ADV",
                "S204_SOL_1ST_CLAMP6_RET",
                "S204_SOL_1ST_PIN1_ADV",
                "S204_SOL_1ST_PIN1_RET",
                "S204_SOL_1ST_PIN2_ADV",
                "S204_SOL_1ST_PIN2_RET",
                "S204_SOL_2ND_LATCH1_ADV",
                "S204_SOL_2ND_LATCH1_RET",
                "S204_SOL_2ND_LATCH2_ADV",
                "S204_SOL_2ND_LATCH2_RET",
                "S204_SOL_2ND_PIN_ADV",
                "S204_SOL_2ND_PIN_RET",
                "S204_SOL_CT_1ST_C_TYPE_LATCH1_ADV",
                "S204_SOL_CT_1ST_C_TYPE_LATCH1_RET",
                "S204_SOL_CT_1ST_C_TYPE_LATCH2_ADV",
                "S204_SOL_CT_1ST_C_TYPE_LATCH2_RET",
                "S204_SOL_CT_1ST_C_TYPE_LATCH3_ADV",
                "S204_SOL_CT_1ST_C_TYPE_LATCH3_RET",
                "S204_SOL_CT_1ST_C_TYPE_LATCH4_ADV",
                "S204_SOL_CT_1ST_C_TYPE_LATCH4_RET",
                "S204_SOL_CT_1ST_CLAMP1_ADV",
                "S204_SOL_CT_1ST_CLAMP1_RET",
                "S204_SOL_CT_1ST_CLAMP2_ADV",
                "S204_SOL_CT_1ST_CLAMP2_RET",
                "S204_SOL_CT_1ST_CLAMP3_ADV",
                "S204_SOL_CT_1ST_CLAMP3_RET",
                "S204_SOL_CT_1ST_CLAMP4_ADV",
                "S204_SOL_CT_1ST_CLAMP4_RET",
                "S204_SOL_CT_1ST_PIN1_ADV",
                "S204_SOL_CT_1ST_PIN1_RET",
                "S204_SOL_CT_2ND_LATCH1_ADV",
                "S204_SOL_CT_2ND_LATCH1_RET",
                "S204_SOL_TT_LOCK_ADV",
                "S204_SOL_TT_LOCK_RET",
                "S204_TURN_SRV_SAFTY1",
                "S204_TURN_SRV_SAFTY2",
                "S205_RBT1_OUTPUT_ADDRESS",
                "S205_RBT2_OUTPUT_ADDRESS",
                "S205_RBT3_OUTPUT_ADDRESS",
                "S205_RBT4_OUTPUT_ADDRESS",
                "S205_RBT5_OUTPUT_ADDRESS",
                "S205_SOL_2ND_LATCH1_ADV",
                "S205_SOL_2ND_LATCH1_RET",
                "S205_SOL_C_TYPE_LATCH1_ADV",
                "S205_SOL_C_TYPE_LATCH1_RET",
                "S205_SOL_C_TYPE_LATCH2_ADV",
                "S205_SOL_C_TYPE_LATCH2_RET",
                "S205_SOL_C_TYPE_LATCH3_ADV",
                "S205_SOL_C_TYPE_LATCH3_RET",
                "S205_SOL_CLAMP1_ADV",
                "S205_SOL_CLAMP1_RET",
                "S205_SOL_CLAMP2_ADV",
                "S205_SOL_CLAMP2_RET",
                "S205_SOL_CT_2ND_LATCH1_ADV",
                "S205_SOL_CT_2ND_LATCH1_RET",
                "S205_SOL_CT_C_TYPE_LATCH1_ADV",
                "S205_SOL_CT_C_TYPE_LATCH1_RET",
                "S205_SOL_CT_C_TYPE_LATCH2_ADV",
                "S205_SOL_CT_C_TYPE_LATCH2_RET",
                "S205_SOL_CT_C_TYPE_LATCH3_ADV",
                "S205_SOL_CT_C_TYPE_LATCH3_RET",
                "S205_SOL_CT_CLAMP1_ADV",
                "S205_SOL_CT_CLAMP1_RET",
                "S205_SOL_CT_CLAMP2_ADV",
                "S205_SOL_CT_CLAMP2_RET",
                "S205_SOL_CT_PIN_ADV",
                "S205_SOL_CT_PIN_RET",
                "S205_SOL_PIN_ADV",
                "S205_SOL_PIN_RET",
                "SA_Q_ERROR",
                "SA_Q_ERROR_RESET_SIG",
                "SA_Q_ROBOT_IN_OK",
                "SA_Q_ROBOT_WAIT_IN_OK",
                "SA_Q_S205_RB1_MUTUAL_INT3",
                "SA_Q_S205_RB2_MUTUAL_INT6",
                "SA_Q_S205_RB3_MUTUAL_INT7",
                "SA_Q_S205_RB4_MUTUAL_INT8",
                "SA_Q_S205_RB5_MUTUAL_INT9",
            };
        }

        private void BuildCharColorMap(List<string> names)
        {
            charColors.Clear();
            foreach (var c in names.SelectMany(n => n.ToCharArray()).Distinct())
            {
                charColors[c] = GetColorFromChar(c);
            }
        }

        private Color GetColorFromChar(char c)
        {
            int index = c % 360;
            float hue = (index * 47) % 360;
            return ColorFromHSV(hue, 0.85f, 0.85f);
        }

        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = System.Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);

            int r = 0, g = 0, b = 0;
            switch (hi)
            {
                case 0: r = (int)value; g = (int)t; b = (int)p; break;
                case 1: r = (int)q; g = (int)value; b = (int)p; break;
                case 2: r = (int)p; g = (int)value; b = (int)t; break;
                case 3: r = (int)p; g = (int)q; b = (int)value; break;
                case 4: r = (int)t; g = (int)p; b = (int)value; break;
                case 5: r = (int)value; g = (int)p; b = (int)q; break;
            }

            return Color.FromArgb(255, Clamp(r), Clamp(g), Clamp(b));
        }

        private int Clamp(double val) => Math.Max(0, Math.Min(255, (int)val));

        private Image RenderGroupedImage(IEnumerable<DeviceApi> items)
        {
            var sorted = items
                .GroupBy(i => i.Group)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderBy(i => i.Device).ThenBy(i => i.Api))
                .ToList();

            int height = sorted.Count * LineHeight + CanvasMargin * 2;
            Bitmap bmp = new(CanvasWidth, height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            // Draw group background
            int index = 0;
            while (index < sorted.Count)
            {
                var current = sorted[index];
                string currentGroup = current.Group;
                Color groupColor = Color.FromArgb(current.Color);

                int start = index;
                while (index + 1 < sorted.Count && sorted[index + 1].Group == currentGroup)
                    index++;

                int blockY = CanvasMargin + start * LineHeight;
                int blockHeight = (index - start + 1) * LineHeight;
                using var bgBrush = new SolidBrush(Color.FromArgb(25, groupColor));
                g.FillRectangle(bgBrush, 0, blockY, CanvasWidth, blockHeight);

                index++;
            }

            // Draw text per tag with character-based color
            int y = CanvasMargin;
            foreach (var item in sorted)
            {
                int x = CanvasMargin;
                foreach (char c in item.Tag)
                {
                    if (!charColors.TryGetValue(c, out var color)) color = Color.Black;
                    using var brush = new SolidBrush(color);
                    g.DrawString(c.ToString(), DrawFont, brush, x, y + 2);
                    x += 14;
                }

                y += LineHeight;
            }

            return bmp;
        }
    }
}
