using Dual.Web.Blazor.Client;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dual.Web.Blazor.Client.Canvas2d;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

/// <summary>
/// JSInterop for Canvas
/// </summary>
public class CanvasJsInterop : DualWebBlazorJsInterop
{
    protected Lazy<Task<IJSObjectReference>> moduleCanvasTask;
    public CanvasJsInterop(IJSRuntime jsRuntime)
        : base(jsRuntime)
    {
        moduleCanvasTask = new(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Dual.Web.Blazor.Client.Canvas2d/CanvasHelper.js" )
            .AsTask());
    }


    public async ValueTask<WindowDimension> GetCanvasDimension(string holderName)
    {
        var module = await moduleCanvasTask.Value;
        return await module.InvokeAsync<WindowDimension>("getCanvasDimension", holderName);
    }
    public async ValueTask DisposeCanvas(string canvasHolderName)
    {
        var module = await moduleCanvasTask.Value;
        await module.InvokeVoidAsync("disposeCanvas", canvasHolderName);
    }
}
