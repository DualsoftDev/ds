using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static partial class EmControl
    {
        /// <summary>
        /// 컨트롤의 배경색을 깜빡이는 애니메이션을 시작합니다.
        /// </summary>
        public static IDisposable BreatheBackground(this Control control, Color startColor, Color endColor)
        {
            var timer = new Timer() { Interval = 100 };
            int steps = 30; // 애니메이션 단계 수
            int currentStep = 0;
            bool forward = true; // start -> end 또는 end -> start 방향

            timer.Tick += (s, e) =>
            {
                float progress = (float)currentStep / steps;
                control.BackColor = interpolateColor(startColor, endColor, progress);

                if (forward) currentStep++;
                else currentStep--;

                if (currentStep >= steps) forward = false; // 끝까지 갔으면 역방향
                else if (currentStep <= 0) forward = true; // 처음까지 갔으면 정방향
            };

            timer.Start();

            return Disposable.Create(() =>
            {
                timer.Stop();
                timer.Dispose();
            });

            Color interpolateColor(Color from, Color to, float progress)
            {
                int r = (int)(from.R + (to.R - from.R) * progress);
                int g = (int)(from.G + (to.G - from.G) * progress);
                int b = (int)(from.B + (to.B - from.B) * progress);

                return Color.FromArgb(r, g, b);
            }
        }

        /// <summary>
        /// 컨트롤의 배경색을 깜빡이는 애니메이션을 시작합니다.
        /// </summary>
        public static IDisposable BreatheBackground(this Control control) => control.BreatheBackground(Color.Yellow, Color.Green);


        /// <summary>
        /// 컨트롤의 배경색을 깜빡이게 합니다.
        /// </summary>
        /// <param name="control">대상 컨트롤</param>
        /// <param name="blinkingColor">깜빡일 색상</param>
        /// <returns>깜빡임 애니메이션을 중지하는 IDisposable</returns>
        public static IDisposable BlinkBackground(this Control control, Color blinkingColor)
        {
            var timer = new Timer { Interval = 500 }; // 깜빡임 주기 (ms)
            var originalColor = control.BackColor;   // 원래 배경색 저장
            bool isBlinking = false;                 // 현재 색상 상태 플래그

            timer.Tick += (s, e) =>
            {
                // 현재 색상 상태에 따라 색상을 전환
                control.BackColor = isBlinking ? originalColor : blinkingColor;
                isBlinking = !isBlinking;
            };

            timer.Start();

            return Disposable.Create(() =>
            {
                // 깜빡임 중지 및 원래 색상 복원
                timer.Stop();
                timer.Dispose();
                control.BackColor = originalColor;
            });
        }
        public static IDisposable BlinkBackground(this Control control) => control.BlinkBackground(Color.DarkOrange);

    }
}
