namespace Dual.Web.Blazor.Client.Components;

public enum MouseState
{
    None,
    Down,
    Up,
    Move,
}

public interface IMouseEventArgs {
    string EventName { get; }
    MouseState MouseState { get; }
    int ScreenX { get; }
    int ScreenY { get; }
    int ClientX { get; }
    int ClientY { get; }
    int MovementX { get; }
    int MovementY { get; }
    int OffsetX { get; }
    int OffsetY { get; }
    bool AltKey { get; }
    bool CtrlKey { get; }
    bool Bubbles { get; }
    int Buttons { get; }
    int Button { get; }
}

// built from function jsMouseArgs
public class JSMouseArgs : IMouseEventArgs
{
    public string EventName { get; set; }
    public MouseState MouseState { get; set; }
    public int ScreenX { get; set; }
    public int ScreenY { get; set; }
    public int ClientX { get; set; }
    public int ClientY { get; set; }
    public int MovementX { get; set; }
    public int MovementY { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public bool AltKey { get; set; }
    public bool CtrlKey { get; set; }
    public bool Bubbles { get; set; }
    public int Buttons { get; set; }
    public int Button { get; set; }
}
