using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace OPC.DSClient
{
    [DebuggerDisplay("{Name}")]
    public class OpcDsTag : INotifyPropertyChanged
    {
        public override string ToString()
        {
            return QualifiedName;
        }

        private object _value = "N/A"; // 기본값 설정
        private DateTime _timestamp = DateTime.Now; // 기본값 설정
        [Browsable(false)]
        public string TagKindDefinition { get; set; } = string.Empty; // 기본값 설정

        public string Path { get; set; } = string.Empty; // 기본값 설정

        [Browsable(false)]
        public string ParentPath { get; set; } = string.Empty; // 기본값 설정


        [Browsable(false)]
        public bool IsFolder { get; set; } = false;

        [Browsable(false)]
        public int Count { get; set; } = 0;

        [Browsable(false)]
        public float MovingSTD { get; set; } = 0.0f;
        [Browsable(false)]
        public float MovingAVG { get; set; } = 0.001f; // 평균값 기본 1msec 설정 UI 영역때문에

        [Browsable(false)]
        public uint MovingDuration { get; set; } = 0;

        [Browsable(false)]
        public List<uint> MovingTimes { get; set; } = new List<uint>();

        [Browsable(false)]
        public uint WaitingDuration { get; set; } = 0;

        [Browsable(false)]
        public uint ActiveDuration { get; set; } = 0;

        public string Name { get; set; } = string.Empty; // 기본값 설정
        [Browsable(false)]
        public string QualifiedName { get; set; } = string.Empty; // 기본값 설정
        public object Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public string DataType { get; set; }

        public string Timestamp
        {
            get => _timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            set
            {
                if (DateTime.TryParse(value, out var newTimestamp) && _timestamp != newTimestamp)
                {
                    _timestamp = newTimestamp;
                    OnPropertyChanged(nameof(Timestamp));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            try
            {
                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0]; // 첫 번째 열린 폼 가져오기
                    if (mainForm != null && mainForm.InvokeRequired)
                    {
                                // UI 스레드에서 실행
                        mainForm.Invoke(new Action(() =>
                        {
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                        }));
                        return;
                    }
                }

                // Invoke가 필요 없는 경우 직접 호출
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPropertyChanged error: {ex.Message}");
            }
        }

    }
    public enum TagKindOpc
    {
        ActionIn,
        Finish,
        CalcCount,
        CalcActiveDuration,
        CalcWaitingDuration,
        CalcMovingDuration,
        CalcAverage,
        CalcStandardDeviation
    }
}