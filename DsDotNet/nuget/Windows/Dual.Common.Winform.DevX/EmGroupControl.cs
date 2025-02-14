using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using DevExpress.Utils.Drawing;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.ButtonsPanelControl;

namespace Dual.Common.Winform.DevX
{
    public static class EmGroupControl
    {
        static Dictionary<GroupControl, Tuple<IDisposable, int>> _collapableDic = new Dictionary<GroupControl, Tuple<IDisposable, int>>();
        /// <summary>
        /// GroupControl 을 축소/확대 가능하게 만듭니다.
        /// Designer 를 통해 이미 GroupControl 의 CustomHeaderButtons 에 이미지를 추가한 경우 사용
        /// <br/> headerHeight: collapse 시의 header height
        /// </summary>
        /// <param name="gc"></param>
        public static void MakeCollapsable(this GroupControl gc, int additionalHeaderHeight=0, bool makeDockTop=true)
        {
            if (makeDockTop)
                gc.Dock = DockStyle.Top;

            if (_collapableDic.ContainsKey(gc))
                return;


            // CustomButtonClick 이벤트에 RX throttle 적용
            IDisposable d =
                Observable.FromEventPattern<EventArgs>(gc, nameof(gc.CustomButtonClick))
                    .Throttle(TimeSpan.FromMilliseconds(300)) // 300ms 동안 중복된 이벤트 무시
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(e =>
                    {
                        var headerHeight = gc.GetCaptionHeight() + additionalHeaderHeight;
                        // 축소/확대 상태 전환
                        if (gc.Height > headerHeight)
                            gc.Height = headerHeight; // 축소
                        else
                        {
                            var (_disposable, fullHeight) = _collapableDic[gc];
                            gc.Height = fullHeight; // 복원 (기본값: 200)
                        }
                    });

            int fullHeight = gc.Height; // 현재 높이를 저장
            _collapableDic.Add(gc, Tuple.Create(d, gc.Height));
        }

        public static void MakeCollapsable(this GroupControl gc, Image image, int additionalHeaderHeight=0, bool makeDockTop=true, string text="")
        {
            // 디자이너에서 gridcontrol 객체 선택 후, 속성에서 CustomHeaderButtons 우측 버튼 클릭 후,
            // Items > ImageOptions > Image 에서 선택 가능
            gc.CustomHeaderButtons.Add(new GroupBoxButton(text, image));
            gc.MakeCollapsable(additionalHeaderHeight, makeDockTop);
        }

        public static int GetCaptionHeight(this GroupControl groupControl)
        {
            if (groupControl == null) throw new ArgumentNullException(nameof(groupControl));

            // GroupControl의 내부 viewInfo 필드 접근
            Type panelType = typeof(PanelControl);
            FieldInfo viewInfoField = panelType.GetField("viewInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            if (viewInfoField != null)
            {
                // viewInfo에서 CaptionBounds 가져오기
                var viewInfo = viewInfoField.GetValue(groupControl) as GroupObjectInfoArgs;
                if (viewInfo != null)
                {
                    // CaptionBounds의 높이 반환
                    return viewInfo.CaptionBounds.Height;
                }
            }

            throw new InvalidOperationException("Unable to retrieve caption height from GroupControl.");
        }
    }
}
