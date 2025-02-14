/*
 * Adapted from Scott Harden's EXCELLENT blog post,
 * "Draw Animated Graphics in the Browser with Blazor WebAssembly"
 * https://swharden.com/blog/2021-01-07-blazor-canvas-animated-graphics/
 */

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using Dual.Common.Core;
using Dual.Web.Blazor.Client.Canvas2d.Model;
using Dual.Web.Blazor.Client.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

using System.Diagnostics;
using System.Drawing;
using Dual.Web.Blazor.Canvas;

namespace Dual.Web.Blazor.Client.Canvas2d;

/// <summary>
/// CanvasHelper component gives you render and resize callbacks for Canvas animation
/// </summary>
public class CanvasHelper : ComponentBase, IAsyncDisposable
{
    public bool IdCanvasDispose { get; set; }
    /// <summary>
    /// In a Blazor App, you would wrap a BECanvas element
    /// in the CanvasHelper
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public ICanvasDimension CanvasDimension { get; set; }

    [Parameter] public string Name { get; set; }

    /// <summary>
    /// [-] never update
    /// [0] 계속 update (0 ms pause)
    /// [+t] t ms pause 후 update
    /// </summary>
    [Parameter] public int RefreshInterval { get; set; }

    [Parameter] public int WidthPx { get; set; } = 1024;
    [Parameter] public int HeightPx { get; set; } = 768;

    [Parameter] public object BackgroundFillStyle { get; set; } = "#003366";  // e.g color 인 경우, 문자열로 "#003366"
    [Parameter] public List<IDrawable> Drawables { get; set; }// = new();

    /// <summary>
    /// Event called when the browser (and therefore the canvas) is resized
    /// </summary>
    [Parameter] public EventCallback<Size> CanvasResized { get; set; }

    /// <summary>
    /// Event called every time a frame can be redrawn
    /// </summary>
    [Parameter] public EventCallback<double> RenderFrame { get; set; }


    /// <summary>
    /// 사용자가 canvas 에 추가적으로 더 그릴 수 있도록 하는 call back event
    /// </summary>
    [Parameter] public EventCallback<Canvas2DContext> Canvas2dCustomDrawing { get; set; }

    [JSInvokable] public string GetCanvasHolderName() => $"canvasHolder{Name}";
    [JSInvokable] public int GetRefreshInterval() => RefreshInterval;
    [Inject] protected IJSRuntime _jsRuntime { get; set; }
    [Inject] protected CanvasJsInterop JsCanvas { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }

    protected ICanvas Canvas { get; set; }
    protected Size Size = new Size();
    protected double FPS;
    public Canvas2DContext Canvas2DContext { get; set; }
    protected BECanvasComponent RefBECanvas;
    protected CanvasHelper RefCanvasHelper;

    // <canvas></canvas> element 그 자체
    public ElementReference CanvasReference => RefBECanvas.CanvasReference;
    public async Task<string> GetCanvasElementIdAsync()
    {
        var id = await JsCanvas.GetAttribute(CanvasReference, "id");
        return id.ToString();
    }

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
        NavigationManager.LocationChanged += DetectNavigation;
    }

    async void DetectNavigation(object sender, LocationChangedEventArgs e)
    {
        // page 를 벗어 날 때, canvas 를 dispose 해 주어야, canvas drawing routine 이 더이상 실행되지 않는다.
        // invalid canvas 오류 방지.
        IdCanvasDispose = true;
        await JsCanvas.Debug($"Navigation event triggered on CompLayout.razor: {e.Location}");
        await JsCanvas.DisposeCanvas(GetCanvasHolderName());
        NavigationManager.LocationChanged -= DetectNavigation;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // DO *NOT* remove me.
        await Task.Yield();
    }

    protected async Task OnAfterFirstRenderAsync()
    {
        await Console.Out.WriteLineAsync($"CanvasHelper.OnAfterRenderAsync(): Name={Name}, Canvas={Canvas}, RefCanvas={RefBECanvas}, RefCanvasHelper={RefCanvasHelper}");
        Canvas.FillStyle = null;    // BackgroundFillStyle;
        // Create the canvas and context
        Canvas2DContext = await RefBECanvas.CreateCanvas2DAsync();
        // Initialize the helper
        await RefCanvasHelper.Initialize();
    }


    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var sequence = 0;
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "id", GetCanvasHolderName());
        //builder.AddAttribute(sequence++, "style", $"position: relative; width: {WidthPx}px; height: {HeightPx}px");
        builder.AddContent(sequence++, ChildContent);
        builder.CloseElement();
    }


    /// <summary>
    /// JS Interop module used to call JavaScript
    /// </summary>
    Lazy<Task<IJSObjectReference>> _moduleTask;

    /// <summary>
    /// Used to calculate the frames per second
    /// </summary>
    DateTime _lastRender;

    // Video 인 경우, 다른 함수 사용
    protected virtual string initRendererJSName { get; set; } = "initRenderJS";

    /// <summary>
    /// Call this in your Blazor app's OnAfterRenderAsync method when firstRender is true
    /// </summary>
    /// <returns></returns>
    public async Task Initialize()
    {
        // We need to specify the .js file path relative to this code
        _moduleTask = new(() => _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Dual.Web.Blazor.Client.Canvas2d/CanvasHelper.js").AsTask());

        // Load the module
        var module = await _moduleTask.Value;

        // Initialize : initRendererJSName = { "initRenderJS" or "initRenderVideoJS" }
        if (! initRendererJSName.IsOneOf("initRenderJS", "initRenderVideoJS"))
            throw new Exception($"Unknown initRenderJSName: {initRendererJSName}");

        await module.InvokeVoidAsync(initRendererJSName, DotNetObjectReference.Create(this));

        // Dispose the module
        await module.DisposeAsync();
    }

    /// <summary>
    /// Handle the JavaScript event called when the browser/canvas is resized
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    [JSInvokable]
    public async Task ResizeInBlazor(int width, int height)
    {
        var size = new Size(width, height);
        // Raise the CanvasResized event to the Blazor app
        await CanvasResized.InvokeAsync(size);
    }

    /// <summary>
    /// Handle the JavaScript event when a frame is ready to render
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    [JSInvokable]
    public async ValueTask RenderInBlazor(float timeStamp)
    {
        //Console.WriteLine($"RenderInBlazor({timeStamp}): {GetCanvasHolderName()}");

        // calculate frames per second
        double fps = 1.0 / (DateTime.Now - _lastRender).TotalSeconds;

        _lastRender = DateTime.Now; // update for the next time

        // 다음과 같은 exception 이 발생하지만, try-catch 수행하면 오히려 handling 이 안되어서 "Reload" 버튼을 눌러야 한다.
        // "Uncaught (in promise) Error: Microsoft.JSInterop.JSException: Invalid canvas."
        await RenderFrame.InvokeAsync(fps);

        await Task.Delay(50);  // kwak
    }



#pragma warning disable 4014  // warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
    /// <summary>
    /// Called by CanvasHelper whenever the browser is resized.
    /// </summary>
    /// <param name="size"></param>
    public virtual void OnCanvasResized(Size size)
    {
        Console.WriteLine($"OnCanvasResized({size.Width} x {size.Height})");
        Size = size;
        Canvas.Resize(size.Width, size.Height);


        if (CanvasDimension is null)
            return;

        // do *NOT* (a)wait: Cannot wait on monitors on this runtime. at System.Threading.Monitor.ObjWait(Int32 millisecondsTimeout, Object obj)
        JsCanvas.Debug($"CompLayout.razor: OnCanvasResized({size.Width}, {size.Height}).  FactoryLayout: Name={CanvasDimension.Name}");
        var (w, h, W, H) = ((double)size.Width, (double)size.Height, CanvasDimension.W, CanvasDimension.H);

        double wr = w / W;
        double hr = h / H;
        double rr =
            (W > w || H > h)
            ? Math.Min(wr, hr)  // 축소시켜야 함
            : Math.Max(wr, hr)  // 확대
            ;

        double ww = W * rr;
        double hh = H * rr;

        // do *NOT* (a)wait: Cannot wait on monitors on this runtime. at System.Threading.Monitor.ObjWait(Int32 millisecondsTimeout, Object obj)
        JsCanvas.Warn($"( w x h = {w} x {h}, W x H = {W} x {H}, rr = {rr}, ww x hh = {ww} x {hh}");
        (CanvasDimension.w, CanvasDimension.h) = (ww, hh);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask != null && _moduleTask.IsValueCreated)
        {
            await Console.Out.WriteLineAsync($"Disposing canvas {Name}");
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
        else
            await Console.Out.WriteLineAsync($"Skipping disposing canvas rendering for {Name}");
    }
}
