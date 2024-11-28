using DevExpress.Charts.Heatmap;
using DevExpress.Utils;
using DevExpress.XtraCharts.Heatmap;
using System;
using System.ComponentModel;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    public static class HeatmapManager
    {
        public static double ScaleUnit = 1.0;
        public static void InitializeHeatmapColorProvider(HeatmapControl heatmapControl)
        {
            // 색상 팔레트 정의 (10단계)
            var palette = new DevExpress.XtraCharts.Palette("ExtendedVariancePalette")
            {
                Color.FromArgb(105, 168, 204), // 1단계
                Color.FromArgb(125, 205, 168), // 2단계
                Color.FromArgb(155, 215, 155), // 3단계
                Color.FromArgb(180, 224, 149), // 4단계
                Color.FromArgb(215, 225, 135), // 5단계
                Color.FromArgb(253, 204, 138), // 6단계
                Color.FromArgb(251, 167, 86),  // 7단계
                Color.FromArgb(225, 123, 49),  // 8단계
                Color.FromArgb(199, 73, 25),   // 9단계
                Color.FromArgb(180, 43, 1)     // 10단계
            };

            // 색상 공급자 설정
            var colorProvider = new HeatmapRangeColorProvider
            {
                Palette = palette,
                ApproximateColors = true // 색상 보간 활성화
            };

            // 범위 설정
            double maxVariance = 100; // 최대 Variance 값
            double step = maxVariance / (palette.Count);

            for (double i = 0; i <= maxVariance; i += step)
            {
                colorProvider.RangeStops.Add(new HeatmapRangeStop(Math.Round(i, 2), HeatmapRangeStopType.Absolute));
            }

            heatmapControl.ColorProvider = colorProvider;
        }

        public static void UpdateHeatmap(HeatmapControl heatmapControl, List<OpcDsTag> opcTags)
        {

            int count = opcTags.Count;
            int width = (int)Math.Ceiling(Math.Sqrt(count));
            int height = width;

            double[] xArguments = new double[width];
            double[] yArguments = new double[height];
            double[,] values = new double[height, width];

            for (int i = 0; i < width; i++) xArguments[i] = i;
            for (int i = 0; i < height; i++) yArguments[i] = i;

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (index < count)
                    {
                        var tag = opcTags[index++];

                        values[y, x] = Math.Round(tag.MovingSTD/1000.0 * ScaleUnit, 2); //  값을 소수점 두 자리로 반올림
                    }
                    else
                    {
                        values[y, x] = 0;
                    }
                }
            }

            var adapter = new HeatmapMatrixAdapter
            {
                XArguments = xArguments,
                YArguments = yArguments,
                Values = values
            };

            heatmapControl.DataAdapter = adapter;
            heatmapControl.Refresh();
        }

    }

    internal class RandomHelper
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// v1과 v2 사이의 난수를 반환합니다.
        /// </summary>
        /// <param name="v1">최소값</param>
        /// <param name="v2">최대값</param>
        /// <returns>v1과 v2 사이의 난수를 반환</returns>
        internal static double GetRandomDouble(int v1, int v2)
        {
            if (v1 > v2)
            {
                throw new ArgumentException("v1은 v2보다 작거나 같아야 합니다.");
            }

            return _random.NextDouble() * (v2 - v1) + v1;
        }
    }
}
