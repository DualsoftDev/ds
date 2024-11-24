using DevExpress.Entity.Model.Metadata;
using DevExpress.XtraTreeMap;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Windows.Forms;
using static DevExpress.Data.Mask.MaskManager;

namespace OPC.DSClient.WinForm.UserControl
{
    /// <summary>
    /// SunburstControl의 회전을 처리하는 헬퍼 클래스
    /// </summary>
    public class SunburstRotationHelper
    {
        private bool isRotating;
        private Point lastMousePosition;
        private readonly SunburstControl sunburst;

        public event EventHandler<double>? Rotate;

        public SunburstRotationHelper(SunburstControl sunburst)
        {
            this.sunburst = sunburst;
            sunburst.MouseDown += OnMouseDown;
            sunburst.MouseMove += OnMouseMove;
            sunburst.MouseUp += OnMouseUp;
            sunburst.MouseDoubleClick += OnMouseDoubleClick;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            var hitInfo = sunburst.CalcHitInfo(e.Location);
            //if (hitInfo?.InSunburstItem == true)
            if (hitInfo != null)
            {
                isRotating = true;
                lastMousePosition = e.Location;
                sunburst.Cursor = Cursors.Hand;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isRotating) return;

            var angleDelta = CalculateAngleDelta(lastMousePosition, e.Location);
            var newAngle = (sunburst.StartAngle + angleDelta) % 360;
            var newSetting = Convert.ToInt32(newAngle < 0 ? newAngle + 360 : newAngle);
            sunburst.StartAngle = newSetting;
            lastMousePosition = e.Location;
            Rotate?.Invoke(this, sunburst.StartAngle);
        }

      
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            isRotating = false;
            sunburst.Cursor = Cursors.Default;

        }


        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Sunburst의 클릭 위치 정보 계산
            SunburstHitInfo hitInfo = sunburst.CalcHitInfo(e.Location);
            if (!(sunburst.DataAdapter is SunburstHierarchicalDataAdapter adapter))
                return;

            if (hitInfo.InSunburstItem)
            {
                HandleSunburstItemClick(hitInfo, adapter);
            }
            else if (hitInfo.InCenterLabel && dataStack.Count > 0)
            {
                RestorePreviousDataSource(adapter);
            }
        }
        /// <summary>
        /// Sunburst 항목 클릭 처리
        /// </summary>
        private void HandleSunburstItemClick(SunburstHitInfo hitInfo, SunburstHierarchicalDataAdapter adapter)
        {
            if (hitInfo.SunburstItem.Tag is DsUnit si && si.DsUnits.Count > 0)
            {
                var drillDownDataSource = si.DsUnits;
                if (drillDownDataSource != null && adapter.DataSource != drillDownDataSource)
                {
                    // 현재 데이터 소스를 Stack에 저장
                    dataStack.Push(new DataSourceInfo(adapter.DataSource, sunburst.CenterLabel.TextPattern));

                    // 새로운 데이터 소스로 전환
                    adapter.DataSource = drillDownDataSource;

                    // UI 갱신
                    sunburst.Refresh();
                }
            }
        }

        /// <summary>
        /// 이전 데이터 소스로 복원
        /// </summary>
        private void RestorePreviousDataSource(SunburstHierarchicalDataAdapter adapter)
        {
            DataSourceInfo sourceInfo = dataStack.Pop();

            // 이전 데이터 소스 복원
            adapter.DataSource = sourceInfo.Source;

            // CenterLabel 텍스트 복원
            sunburst.CenterLabel.TextPattern = sourceInfo.Label;

            // UI 갱신
            sunburst.Refresh();
        }

        // DataSourceInfo 클래스
        public class DataSourceInfo
        {
            public object Source { get; }
            public string Label { get; }

            public DataSourceInfo(object source, string label)
            {
                Source = source;
                Label = label;
            }
        }

        // DataStack 필드
        private readonly Stack<DataSourceInfo> dataStack = new Stack<DataSourceInfo>();



        private double CalculateAngleDelta(Point start, Point end)
        {
            var center = new Point(sunburst.Width / 2, sunburst.Height / 2);
            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
            return (endAngle - startAngle) * (180 / Math.PI);
        }
    }

    public class DsUnit
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; } // 면적 값
        public Color Color { get; set; } // 색상 값 
        public List<DsUnit> DsUnits { get; set; } = new List<DsUnit>();
    }
}
