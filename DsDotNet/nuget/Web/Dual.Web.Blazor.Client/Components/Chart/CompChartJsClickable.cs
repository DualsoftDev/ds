using Microsoft.JSInterop;

namespace Dual.Web.Blazor.Client.Components.Chart;

public class ChartJsClickInfo
{
    /// DatasetLabel
    public string Label { get; set; }
    public int DataSetIndex { get; set; }
    public int ElementIndex { get; set; }
    public double Value { get; set; }
}

public class ChartJsClickHandler : IDisposable
{
    public DotNetObjectReference<ChartJsClickHandler> DotNetObjectReference { get; set; }
    Action<ChartJsClickInfo> _onClick { get; set; }

    public ChartJsClickHandler(Action<ChartJsClickInfo> onClick)
    {
        _onClick = onClick;
        DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference.Create(this);
    }
    [JSInvokable]
    public void Clicked(string label, int dataSetIndex, int elementIndex, double value)
    {
        _onClick?.Invoke(new ChartJsClickInfo() { Label = label, DataSetIndex = dataSetIndex, ElementIndex = elementIndex, Value = value });
    }

    public void Dispose() => DotNetObjectReference?.Dispose();
}
