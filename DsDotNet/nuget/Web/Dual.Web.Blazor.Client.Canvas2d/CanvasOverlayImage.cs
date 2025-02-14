using Dual.Common.Core;

using Microsoft.AspNetCore.Components;

using System.Drawing;

namespace Dual.Web.Blazor.Client.Canvas2d;

public class CanvasOverlayImage: CanvasOverlay
{
    [Parameter] public string BackgroundImageUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        BackgroundMediaUrl = BackgroundImageUrl;
        await base.OnInitializedAsync();
    }

    protected async Task SetBackgroundImageAsync()
    {
        if (BackgroundImageUrl.IsNullOrEmpty())
            return;

        await Console.Out.WriteLineAsync("****SetBackgroundImage()");
        // TODO: https://stackoverflow.com/questions/58280795/how-can-i-change-css-directlywithout-variable-in-blazor
        // RefBECanvas 가 null 인 상태...
        var holderName = GetCanvasHolderName();      // holderName 아래에 "canvas" class

        await JsCanvas.SetStyle(CanvasReference, "background", $"url('{BackgroundImageUrl}')");
        await JsCanvas.SetStyle(CanvasReference, "backgroundSize", "contain");  // "cover"
        await JsCanvas.SetStyle(CanvasReference, "background-repeat", "no-repeat");
        await JsCanvas.SetAttribute(CanvasReference, "padding", "solid red 2px;");

        Console.WriteLine("<<<<< SetBackgroundImage()");
    }

#pragma warning disable 4014  // warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
    public override async void OnCanvasResized(Size size)
    {
        await Task.Yield();
        base.OnCanvasResized(size);
        // do *NOT* (a)wait: Cannot wait on monitors on this runtime. at System.Threading.Monitor.ObjWait(Int32 millisecondsTimeout, Object obj)
        SetBackgroundImageAsync();
    }
}
