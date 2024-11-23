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
        internal static Color GetColor(OpcTag opcTag, bool bOn)
        {
            return opcTag.TagKindDefinition switch
            {
                /// Flow TagKind
                "idle_mode" or "homing" when bOn => Color.Gray,
                "drive_state" or "going_state" when bOn => Color.Blue,
                "error_state" or "rxErrShort" or "rxErrOpen" or "rxErrInterlock" or "workErrOriginGoing" or "errorTRx" when bOn => Color.Red,
                "emergency_state" when bOn => Color.Orange,
                "ready_state" or "ready" or "going" or "finish" when bOn => Color.Green,
                "pause_state" or "pause" when bOn => Color.Salmon,

                /// Vertex TagKind
                "txErrOnTimeUnder" or "txErrOnTimeOver" or "txErrOffTimeUnder" or "txErrOffTimeOver" when bOn => Color.Brown,

                /// Device TagKind
                "actionIn" => bOn ? Color.Green : Color.Gray,

                /// 기본값
                _ => Color.Transparent
            };
        }

    }
}
