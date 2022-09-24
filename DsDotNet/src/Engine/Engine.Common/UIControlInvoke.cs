using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Engine.Common;

// https://stackoverflow.com/questions/93744/most-common-c-sharp-bitwise-operations-on-enums
/*
    SomeType value = SomeType.Grapes;
    bool isGrapes = value.Is(SomeType.Grapes); //true
    bool hasGrapes = value.Has(SomeType.Grapes); //true

    value = value.Add(SomeType.Oranges);
    value = value.Add(SomeType.Apples);
    value = value.Remove(SomeType.Grapes);

    bool hasOranges = value.Has(SomeType.Oranges); //true
    bool isApples = value.Is(SomeType.Apples); //false
    bool hasGrapes = value.Has(SomeType.Grapes); //false
*/
public static class UIControlInvoke
{
    public static async Task DoAsync(this System.Windows.Forms.Control control, Action action)
    {
        try
        {
            if (control.InvokeRequired)
            {
                await Task.Factory.FromAsync(control.BeginInvoke(action), result => { });
            }
            else if (control.IsHandleCreated)
            {
                action();
            }
            else
            {
                Console.WriteLine("Error : 창 핸들을 만들기 전까지는....");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Exception on Control.Do(): {ex}");
        }
    }

    /// <summary>
    /// Control.Invoke is synchronous
    /// </summary>
    /// <param name="control"></param>
    /// <param name="action"></param>
    public static void Do(this System.Windows.Forms.Control control, Action action, Action<System.Windows.Forms.Control> onError = null)
    {
        try
        {
            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(action);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException)
                    {
                        Trace.WriteLine(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (control.IsHandleCreated)
            {
                action();
            }
            else
            {
                if (onError == null)
                {
                    Console.WriteLine("Error : Before windows handle created (2).. 창 핸들을 만들기 전까지는....");
                }
                else
                {
                    onError(control);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Exception on Control.Do(): {ex}");
        }
    }
}

