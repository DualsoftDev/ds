using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

[DebuggerDisplay("{Name}")]
public class OpcTag : INotifyPropertyChanged
{
    private object _value = "N/A"; // 기본값 설정
    private DateTime _timestamp = DateTime.Now; // 기본값 설정
    private int _changeCount; // 값 변경 횟수
    [Browsable(false)]
    public string TagKindDefinition { get; set; } = string.Empty; // 기본값 설정

    public string Path { get; set; } = string.Empty; // 기본값 설정

    [Browsable(false)]
    public string ParentPath { get; set; } = string.Empty; // 기본값 설정

    [Browsable(false)]
    public bool IsFolder { get; set; } = false;

    public string Name { get; set; } = string.Empty; // 기본값 설정

    public object Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                _changeCount++; // 변경 횟수 증가
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(ChangeCount)); // 변경 횟수 속성 알림
            }
        }
    }

    [Browsable(false)]
    public int ChangeCount
    {
        get => _changeCount;
        private set
        {
            if (_changeCount != value)
            {
                _changeCount = value;
                OnPropertyChanged(nameof(ChangeCount));
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
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0]?.InvokeRequired == true)
            {
                Application.OpenForms[0]?.Invoke(new Action(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnPropertyChanged error: {ex.Message}");
        }
    }
}
