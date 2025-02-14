using Dual.Common.Core;
using Dual.Web.Blazor.Client.Canvas2d.Model;
using Dual.Web.Blazor.Client;

using Microsoft.AspNetCore.Components;

namespace Dual.Web.Blazor.Client.Canvas2d;

public class CanvasOverlay : CanvasHelper
{
    protected bool _canvasSizeDecided;
    [Parameter] public string BackgroundMediaUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Console.WriteLine($"-------- OnInitializedAsync: Creating [{Name}]");
        var mediaUrl = BackgroundMediaUrl;
        if (mediaUrl.NonNullAny() )
        {
            // imageURL 의 image 가져와서 이미지의 width, height 를 구한 후, canvas 크기를 여기에 맞게 조정한다.
            Console.WriteLine($"-------- OnParametersSetAsync[{Name}]: Trying fetch background media [{mediaUrl}]");
            WindowDimension dim = await JsCanvas.GetMediaDimension(mediaUrl);
            (WidthPx, var height) = (dim.Width, dim.Height);
            Console.WriteLine($"-------- Finished fetching background media {WidthPx} x {height}");
            double ratio = (double)height / WidthPx;
            HeightPx = (int)(WidthPx * ratio);

            Console.WriteLine($"-------- Adjusting height to {HeightPx}");
        }

        if (Canvas is null)
            Canvas = new Model.Canvas();
        Canvas.Resize(WidthPx, HeightPx);
        _canvasSizeDecided = true;

    }
}
