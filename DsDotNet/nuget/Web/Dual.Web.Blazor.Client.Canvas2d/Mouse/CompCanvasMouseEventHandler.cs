/*
 * Adapted from Scott Harden's EXCELLENT blog post,
 * "Draw Animated Graphics in the Browser with Blazor WebAssembly"
 * https://swharden.com/blog/2021-01-07-blazor-canvas-animated-graphics/
 */

using Dual.Common.Core;
using Dual.Web.Blazor.Client.Canvas2d.Model;
using Dual.Web.Blazor.Client.Components;
using Dual.Web.Blazor.Client.Components.Mouse;

using Microsoft.AspNetCore.Components;

namespace Dual.Web.Blazor.Client.Canvas2d;

/// <summary>
/// Canvas 에 대한 mouse event 를 처리하는 component
/// </summary>
public class CompCanvasMouseEventHandler : CompMouseEventDetector, IAsyncDisposable
{
    [Parameter] public List<IDrawable> Drawables { get; set; }

    [Parameter] public int HoverTriggerDurationMS { get; set; } = 500;

    /// <summary>
    /// Canvas 내의 object(IDrawable)에 대한 Mouse{Enter, Leave} 이벤트
    /// </summary>
    [Parameter] public EventCallback<ExtendedMouseArgs> MouseEnterStateChanged { get; set; }

    /// <summary>
    /// click 으로 현재 선택된 object
    /// </summary>
    public IDrawable SelectedObject { get; set; }

    protected override async Task OnInitializedAsync() { await Task.Yield(); }

    IEnumerable<IDrawable> getObjectsOnPosition(IMouseEventArgs args) =>
        Drawables?.OfType<IFillable>().Where(o => o.IsHit(args.OffsetX, args.OffsetY));


    Timer hoverTimer;
    void resetHoverTimer()
    {
        hoverTimer?.Dispose(); // Ensure the timer is stopped when the mouse is out
        hoverTimer = null;
    }

    async Task printStacksAsync()
    {
        var hitStackContent = HitObjects.Select(d => d.Name).JoinString(", ");
        await Console.Out.WriteLineAsync($"PrintStacks: hitStackContent = {hitStackContent}");
    }


    async Task handleMouseEnterAsync(IDrawable enteredObject, ExtendedMouseArgs args)
    {
        await printStacksAsync();

        if (enteredObject is null)
            return;

        if (HitObjects.IsNullOrEmpty() || HitObjects.Last() != enteredObject)
            HitObjects.Add(enteredObject);

        var lastHit = HitObjects.Last();
        resetHoverTimer();

        var previousLeaveArgs = new ExtendedMouseArgs(lastHit, args, isEnter: false);
        await MouseEnterStateChanged.InvokeAsync(previousLeaveArgs);

        var hoverArgs = new ExtendedMouseArgs(enteredObject, args, isEnter: true);
        await MouseEnterStateChanged.InvokeAsync(hoverArgs);

        hoverTimer = new Timer(async state =>
        {
            hoverArgs.SetEventName("hoverenter");
            await onMouseEvent(hoverArgs);
        }, null, HoverTriggerDurationMS, Timeout.Infinite);
    }

    async Task handleMouseLeaveAsync(IMouseEventArgs args)
    {
        if (HitObjects.IsNullOrEmpty())
        {
            Console.WriteLine("------------ WARN: empty stack");
            return;
        }

        await printStacksAsync();

        IDrawable lastHit = HitObjects.Pop();
        var leaveArgs = new ExtendedMouseArgs(lastHit, args, isEnter: false);
        leaveArgs.SetEventName("hoverexit");

        await MouseEnterStateChanged.InvokeAsync(leaveArgs);

        await onMouseEvent(leaveArgs);
        resetHoverTimer();
    }


    public List<IDrawable> HitObjects { get; private set; } = new();

    protected override async Task onMouseEvent(IMouseEventArgs args_)
    {
        await base.onMouseEvent(args_);

        switch(args_.EventName)
        {
            case "hoverexit":
                await handleMouseLeaveAsync(args_);
                return;
        }

        MouseState state = args_.MouseState;
        var hits = getObjectsOnPosition(args_)?.ToArray();
        if (hits.IsNullOrEmpty())
        {
            if (HitObjects.Count > 0)
            {
                await handleMouseLeaveAsync(args_);
                HitObjects.Clear();
            }
            else
                await Console.Out.WriteLineAsync(":::No hits");
            return;
        }

        await Console.Out.WriteLineAsync($"hits: {hits.Length}, HitObjects: {HitObjects.Count}");
        var args = new ExtendedMouseArgs()
        {
            MouseArgs = args_,
            HitObjects = hits
        };

        if (HitObjects.Count == hits.Length)
        {
            //await Console.Out.WriteLineAsync("No change!!");
        }
        else if (HitObjects.Count < hits.Length)
        {
            //await Console.Out.WriteLineAsync("New hits!!");

            /* hit 이 있으면, line(rectangle) 보다는 asset(circle) 선호 */
            var hit = hits.FirstOrDefault(h => h is ICircle);
            hit ??= hits.FirstOrDefault();
            await handleMouseEnterAsync(hit, args);

            if (state == MouseState.Down)
                SelectedObject = hit;
        }
        else
        {
            await Console.Out.WriteLineAsync("What's the hell??");
            await handleMouseLeaveAsync(args_);

            if (state == MouseState.Down)
                SelectedObject = null;
        }
        HitObjects = hits.ToList();


        /* for debugging */
        //var hitObjectNames =
        //    hits.Any()
        //    ? hits.Select(o => $"{o.Name}({o}, {o.Tag})").JoinString(", ")
        //    : "No hit"
        //    ;
        //Console.WriteLine($"Mouse{state} on {TargetElementId} => {hitObjectNames}");

        args.SetMouseState(state);
        hits.Iter(h => h.OnMouse(args));
    }


    public async ValueTask DisposeAsync() { await Task.Yield(); }
}
