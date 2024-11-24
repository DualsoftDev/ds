using DevExpress.TreeMap;
using DevExpress.XtraTreeMap;
using System.Drawing;

namespace OPC.DSClient.WinForm.UserControl
{
    public class DsSunburstPaletteColorizer : SunburstPaletteColorizer
    {
        public DsSunburstPaletteColorizer()
        {
            // 상태별 색상을 가진 Custom Palette 생성
            var customPalette = new Palette
            {
                Color.FromArgb(169, 169, 169),  // : 회색
            };
            // Custom Palette를 Colorizer에 설정
            Palette = customPalette;
        }

        protected override Color GetItemColor(ISunburstItem item, SunburstItemGroupInfo group)
        {
            if (item.Tag is SunInfo sunInfo)
                return sunInfo.Color;
            else return base.GetItemColor(item, group);
        }

    }

    public static class SunburstColor
    {
        private static readonly Dictionary<string, Color> FlowColors = new()
        {
            { "drive_state", Color.Blue },
            { "error_state", Color.Red },
            { "emergency_state", Color.Orange },
            { "pause_state", Color.Salmon },
            { "idle_mode", Color.Gray },
        };

        private static readonly Dictionary<string, Color> VertexColors = new()
        {
            { "ready", Color.Green },
            { "going", Color.Green },
            { "finish", Color.Green },
            { "homing", Color.Green },
            { "pause", Color.Salmon },
            { "txErrOnTimeUnder", Color.Red },
            { "txErrOnTimeOver", Color.Red },
            { "txErrOffTimeOver", Color.Red },
            { "txErrOffTimeUnder", Color.Red },
            { "rxErrShort", Color.Red },
            { "rxErrOpen", Color.Red },
            { "rxErrInterlock", Color.Red },
            { "workErrOriginGoing", Color.Red },
            { "errorTRx", Color.Red },
        };

        private static readonly Dictionary<string, Color> DeviceColors = new()
        {
            { "actionIn", Color.Green },
            { "actionOut", Color.Blue }
        };

        /// <summary>
        /// TagKind에 따라 적절한 색상을 반환합니다.
        /// </summary>
        /// <param name="opcTag">현재 OPC 태그</param>
        /// <param name="bOn">태그 활성 상태</param>
        /// <returns>적절한 Color</returns>
        internal static Color GetFolderColor(OpcTag opcTag, bool bOn)
        {
            // Flow TagKind 색상 반환
            if (FlowColors.TryGetValue(opcTag.TagKindDefinition, out var flowColor) && bOn)
            {
                return flowColor;
            }

            // Real, Call Vertex TagKind 색상 반환
            if (VertexColors.TryGetValue(opcTag.TagKindDefinition, out var vertexColor) && bOn)
            {
                return vertexColor;
            }

            // Device TagKind 색상 반환
            if (DeviceColors.TryGetValue(opcTag.TagKindDefinition, out var deviceColor))
            {
                return bOn ? deviceColor : Color.Gray;
            }

            // 기본값: Transparent
            return Color.Transparent;
        }
        private static readonly Dictionary<string, System.Timers.Timer> BlinkTimers = new();



        internal static void StartBlinking(string tagName, SunInfo sunInfo)
        {
            if (!BlinkTimers.ContainsKey(tagName))
            {
                var timer = new System.Timers.Timer(500); // 500ms 간격으로 깜빡임
                timer.Elapsed += (s, e) =>
                {
                    sunInfo.Color = sunInfo.Color == Color.IndianRed ? Color.LightGray : Color.IndianRed;
                };
                BlinkTimers[tagName] = timer;
                timer.Start();
            }
        }

        internal static void StopBlinking(string tagName)
        {
            if (BlinkTimers.TryGetValue(tagName, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                BlinkTimers.Remove(tagName);
            }
        }

        internal static Color GetSubItemColor(OpcTag opcTag, bool bOn)
        {
            if (opcTag.TagKindDefinition.ToLower().Contains("err"))
                return bOn ? Color.IndianRed : Color.LightGray;
            else
                return bOn ? Color.SkyBlue : Color.LightGray;
        }
    }
}
