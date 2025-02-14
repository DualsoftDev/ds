using Microsoft.AspNetCore.Components;

namespace Dual.Web.Blazor.Client;

public static class EventCallbackExtensions
{
    public static async Task InvokeSafeAsync<T>(this EventCallback<T> eventCallback, T arg)
    {
        if (eventCallback.HasDelegate)
            await eventCallback.InvokeAsync(arg);
    }

    public static async Task InvokeSafeAsync(this EventCallback eventCallback)
    {
        if (eventCallback.HasDelegate)
            await eventCallback.InvokeAsync();
    }
}
