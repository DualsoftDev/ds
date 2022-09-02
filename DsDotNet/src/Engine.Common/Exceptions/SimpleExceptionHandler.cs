using System.Threading.Tasks;
using System;

namespace Engine.Common;

public static class SimpleExceptionHandler
{
    // Task.Run(() => ...) 에서 발생하는 exception 은 wait 하지 않으면 catch 되지 않음.

    public static void InstallExceptionHandler()
    {
        void handle(Exception ex) => Log4NetHelper.Logger?.Error($":::: Unhandled exception\r\n{ex}");
        UnhandledExceptionEventHandler exceptionHander = (s, e) =>
        {
            var ex = (Exception)e.ExceptionObject;
            handle(ex);
        };

        EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = (s, e) =>
        {
            // set observerd 처리 안하면 re-throw 됨
            e.SetObserved();
            handle(e.Exception);
        };


        // Add the event handler for handling non-UI thread exceptions to the event:
        AppDomain.CurrentDomain.UnhandledException += exceptionHander;

        // Task 에서 Exception 이 발생하고, 해당 task 내에서 catch 하지 않아서 숨겨질 수 있는 exception
        // https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
        TaskScheduler.UnobservedTaskException += onUnobservedTaskException;
    }
}
