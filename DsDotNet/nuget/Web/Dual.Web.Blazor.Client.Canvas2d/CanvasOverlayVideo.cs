using Microsoft.AspNetCore.Components;

namespace Dual.Web.Blazor.Client.Canvas2d;

public class CanvasOverlayVideo: CanvasOverlay
{
    [Parameter] public string BackgroundVideoUrl { get; set; }
    protected override async Task OnInitializedAsync()
    {
        BackgroundMediaUrl = BackgroundVideoUrl;
        initRendererJSName = "initRenderVideoJS";
        await base.OnInitializedAsync();
    }
}
