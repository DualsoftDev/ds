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
    public class DsUnit()
    {
        public string Label { get; set; } = string.Empty;
        public object Value => OpcDsTag.Value;
        public int Count => OpcDsTag.Count;
        public float ActiveTime => OpcDsTag.ActiveTime;
        public float WaitingTime => OpcDsTag.WaitingTime;
        public float MovingTime => OpcDsTag.MovingTime;
        public float MovingAVG => OpcDsTag.MovingAVG;
        public float MovingSTD => OpcDsTag.MovingSTD;
        public double Area { get; set; } // 면적 정의 값
        public int Level { get; set; } // 폴더 레벨
        public Color Color { get; set; } // 색상 값 
        public List<DsUnit> DsUnits { get; set; } = new List<DsUnit>();
        public OpcDsTag OpcDsTag { get; set; }

    }
}
