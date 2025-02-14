using Dual.Web.Blazor.Client.Canvas2d.Model;
using Dual.Web.Blazor.Client.Components;

namespace Dual.Web.Blazor.Client.Canvas2d;

public class ExtendedMouseArgs : IMouseEventArgs
{
    public ExtendedMouseArgs() {}
    public ExtendedMouseArgs(IDrawable drawable, IMouseEventArgs mouseArgs, bool isEnter)
    {
        Drawable = drawable;
        MouseArgs = mouseArgs;
        IsEnter = isEnter;
    }

    public IDrawable[] HitObjects { get; set; }
    public IDrawable Drawable { get; set; }
    public IMouseEventArgs MouseArgs { get; set; }
    public bool IsEnter { get; set; }


    public string EventName => MouseArgs.EventName;
    public MouseState MouseState => MouseArgs.MouseState;
    public int ScreenX => MouseArgs.ScreenX;
    public int ScreenY => MouseArgs.ScreenY;
    public int ClientX => MouseArgs.ClientX;
    public int ClientY => MouseArgs.ClientY;
    public int MovementX => MouseArgs.MovementX;
    public int MovementY => MouseArgs.MovementY;
    public int OffsetX => MouseArgs.OffsetX;
    public int OffsetY => MouseArgs.OffsetY;
    public bool AltKey => MouseArgs.AltKey;
    public bool CtrlKey => MouseArgs.CtrlKey;
    public bool Bubbles => MouseArgs.Bubbles;
    public int Buttons => MouseArgs.Buttons;
    public int Button => MouseArgs.Button;
}

public static class ExtendedMouseArgsExtension
{
    public static void SetMouseState(this IMouseEventArgs args, MouseState mouseState)
    {
        if (args is JSMouseArgs jsMouseArgs)
            jsMouseArgs.MouseState = mouseState;
        else if (args is ExtendedMouseArgs exMouseArgs)
            exMouseArgs.MouseArgs.SetMouseState(mouseState);
        else
            throw new Exception($"Not yet implemented for type {args.GetType()}");
    }
    public static void SetEventName(this IMouseEventArgs args, string eventName)
    {
        if (args is JSMouseArgs jsMouseArgs)
            jsMouseArgs.EventName = eventName;
        else if (args is ExtendedMouseArgs exMouseArgs)
            exMouseArgs.MouseArgs.SetEventName(eventName);
        else
            throw new Exception($"Not yet implemented for type {args.GetType()}");
    }
}
