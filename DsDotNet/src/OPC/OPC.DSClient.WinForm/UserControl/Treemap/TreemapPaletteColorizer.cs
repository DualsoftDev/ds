using DevExpress.TreeMap;
using DevExpress.XtraTreeMap;
using System.Collections.Generic;
using System.Drawing;

namespace OPC.DSClient.WinForm.UserControl
{
    /// <summary>
    /// Treemap 전용 Palette Colorizer
    /// </summary>
    public class DsTreemapPaletteColorizer : TreeMapPaletteColorizer
    {
        public DsTreemapPaletteColorizer()
        {
        }

        protected override Color GetItemColor(ITreeMapItem item, TreeMapItemGroupInfo group)
        {
            // DsUnit 객체의 색상을 기반으로 노드 색상 결정
            if (item.Tag is DsUnit dsUnit)
                return dsUnit.Color;
            else
                return base.GetItemColor(item, group);
        }
    }

    /// <summary>
    /// Treemap 전용 Color Helper
    /// </summary>
    public static class TreemapColor
    {
        private static readonly Dictionary<string, Color> CategoryColors = new()
        {
            { "error", Color.Red },
            { "going", Color.Orange },
            { "ready", Color.Green },
            { "finish", Color.Blue },
            { "homing", Color.Gray }
        };

        private static readonly Dictionary<string, System.Timers.Timer> BlinkTimers = new();

        /// <summary>
        /// 카테고리에 따라 색상을 반환합니다.
        /// </summary>
        /// <param name="category">카테고리</param>
        /// <returns>적절한 Color</returns>
        internal static Color GetCategoryColor(string category)
        {
            var findName = category.ToLower().Contains("err") ? "error" : category.ToLower();
            if (CategoryColors.TryGetValue(findName, out var color))
                return color;

            return Color.Transparent; // Skip값
        }

       
    }
}
