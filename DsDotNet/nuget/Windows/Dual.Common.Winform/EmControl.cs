using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Dual.Common.Winform
{
    /// <summary>
    /// http://lostechies.com/derickbailey/2011/01/24/asynchronous-control-updates-in-c-net-winforms/
    /// </summary>
    public static partial class EmControl
    {
        public static void Do<TControl>(this TControl control, Action<TControl> action)
            where TControl : Control
        {
            try
            {
                if (control.InvokeRequired)
                {
                    try
                    {
                        control.Invoke(action, control);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ObjectDisposedException)
                            Trace.WriteLine(ex);
                        else
                            throw;
                    }
                }
                else if (control.IsHandleCreated)
                    action(control);
                else
                {
                    Console.WriteLine("Error : Do() Called Before windows handle created.. 창 핸들을 만들기 전까지는....");
                    Trace.WriteLine("Error : Do() Called Before windows handle created.. 창 핸들을 만들기 전까지는....");
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
        public static void Do1(this Control control, Action action, bool ignoreError=false)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    try
                    {
                        if (control.IsHandleCreated)
                            control.Invoke(action);
                        else
                        {
                            Console.WriteLine("Error : Do() Called Before windows handle created (2).. 창 핸들을 만들기 전까지는....");
                            Trace.WriteLine("Error : Do() Called Before windows handle created (2).. 창 핸들을 만들기 전까지는....");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is ObjectDisposedException || ex is ThreadAbortException)
                            Trace.WriteLine(ex);
                        else
                            throw;
                    }
                }
                else if (control.IsHandleCreated)
                    action();
                else if (!ignoreError)
                {
                    Console.WriteLine("Error : Do() Called Before windows handle created (3).. 창 핸들을 만들기 전까지는....");
                    Trace.WriteLine("Error : Do() Called Before windows handle created (3).. 창 핸들을 만들기 전까지는....");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception on Control.Do(): {ex}");
            }
        }
        public static void DoInvoke(this Control control, Action action)
        {
            if (control.IsHandleCreated && control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }

        /*
         * 다음과 같은 형태로 사용
            await control.DoAsync(async tcs =>
            {
                await Task.Delay(3000); // 비동기 작업 수행
                tcs.SetResult(true);    // 비동기 작업 완료 마킹 (결과 무시)
            });
         */
        public static Task DoAsync(this Control control, Action<TaskCompletionSource<object>> action)
        {
            var tcs = new TaskCompletionSource<object>();
            control.BeginInvoke((Action)(() => { action(tcs); }));
            return tcs.Task;
        }


        /*
         * 다음과 같은 형태로 사용
            int count = await control.DoAsyncT<int>(async tcs =>
            {
                await Task.Delay(3000); // 비동기 작업 수행
                tcs.SetResult(succeeded ? 100 : 0);    // 비동기 작업 완료 마킹 및 결과 반환
            });
         */
        public static Task<T> DoAsyncT<T>(this Control control, Action<TaskCompletionSource<T>> action)
        {
            var tcs = new TaskCompletionSource<T>();
            control.BeginInvoke((Action)(() => { action(tcs); }));
            return tcs.Task;
        }


        public static void Do(this SynchronizationContext context, Action action)
        {
            if (context.IsWaitNotificationRequired())
                context.Send(ignore => { action(); }, null);
            else
                action();
        }
        public static Task DoAsync(this SynchronizationContext context, Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            if (context == null)
                action();
            else
                context.Post(ignore => { action(); }, null);

            tcs.SetResult(null);
            return tcs.Task;
        }

        public static SynchronizationContext GetSynchronizationContext(this Control control)
        {
            SynchronizationContext context = null;
            control.DoInvoke(() =>
            {
                context = SynchronizationContext.Current;
            });

            return context;
        }

        public static TaskScheduler GetTaskScheduler(this Control control)
        {
            TaskScheduler scheduler = null;
            control.DoInvoke(() =>
            {
                scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            });

            return scheduler;
        }


        /// <summary>
        /// form 에 대해서, 아직 창 핸들이 만들어 지지 않은 경우, form 이 보여진 후에 해당 action 을 수행한다.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="action"></param>
        public static void DoNowOrLater(this Form form, Action action)
        {
            if (form.InvokeRequired)
                form.Invoke(action);
            else if (form.IsHandleCreated)
                action();
            else
                form.Shown += (sender, args) => { action(); };
        }

        public static async Task DoAsync(this Control _, Task task) => await task;

        [Obsolete("Use DoAsync() instead")]
        public static async Task DoPost(this Control control, Action action)
        {
            await control.DoAsync(tcs => { action(); tcs.SetResult(null); });
        }


        /// <summary>
        /// How to get return value when BeginInvoke/Invoke is called in C#
        /// http://stackoverflow.com/questions/2214002/how-to-get-return-value-when-begininvoke-invoke-is-called-in-c-sharp
        /// </summary>
        public static T DoGet<T>(this Control control, Func<T> func)
        {
            if (control.InvokeRequired)
                return (T)control.Invoke(func);

            return func();
        }

        public static IEnumerable<Control> CollectAncestors(this Control control, bool includeMe = false)
        {
            if (includeMe)
                yield return control;
            if (control == null || control.Parent == null)
                yield break;

            foreach (var ancestor in control.Parent.CollectAncestors(true))
                yield return ancestor;
        }

        public static IEnumerable<Control> CollectAncestorsTo(this Control control, Control stopAncestorControl,
            bool includeMe = false)
        {
            if (includeMe)
                yield return control;

            if (control == stopAncestorControl || control.Parent == null)
                yield break;

            foreach (var ancestor in control.Parent.CollectAncestorsTo(stopAncestorControl, true))
                yield return ancestor;
        }


        public static IEnumerable<Control> CollectChildren(this Control control, bool includeMe = false)
        {
            if (includeMe)
                yield return control;

            foreach (var child in control.Controls.Cast<Control>())
            {
                foreach (var descendant in child.CollectChildren(true))
                    yield return descendant;
            }

            if (control is TabControl)
            {
                var tab = (TabControl)control;
                foreach (var page in tab.TabPages.Cast<TabPage>())
                {
                    foreach (var descendant in page.CollectChildren(true))
                    {
                        yield return descendant;

                    }
                }
            }
        }


        public static IEnumerable<T> CollectChildren<T>(this Control control, bool includeMe = false) where T : Control
        {
            return control.CollectChildren(includeMe).OfType<T>();

            //if (includeMe && control is T)
            //    yield return control as T;

            //foreach (var child in control.Controls.Cast<Control>())
            //{
            //    foreach (var descendant in child.CollectChildren<T>(true))
            //        yield return descendant;
            //}

            //if (control is TabControl)
            //{
            //    var tab = (TabControl)control;
            //    foreach (var page in tab.TabPages.Cast<TabPage>())
            //    {
            //        foreach (var descendant in page.CollectChildren<T>(true))
            //        {
            //            yield return descendant;

            //        }
            //    }
            //}
        }



        /// <summary>
        /// reference 의 LT 좌표 기준으로, control 의 LT 좌표의 offset 값을 반환한다.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Point GetRelativePoint(this Control control, Control reference)
        {
            return reference.PointToClient(control.PointToScreen(control.Location));
        }

        public static Rectangle GetRelativeRectangle(this Control control, Control reference)
        {
            return reference.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
        }

        public static void SetBackgroundImage(this Control control, Image image)
        {
            if (control.BackgroundImage != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                control.BackgroundImage.Dispose();

            control.BackgroundImage = image;
        }

        public static void MakeTransparent(this Control control)
        {
            try
            {
                if (control is Button)
                    ((Button)control).FlatStyle = FlatStyle.Flat;

                control.ForceMakeTransparent();
                control.BackColor = Color.Transparent;
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// http://solvedstack.com/questions/transparency-for-windows-forms-textbox
        /// <c>
        /// bool itWorked = SetStyle(myControl, ControlStyles.SupportsTransparentBackColor, true);
        /// </c>
        /// </summary>
        /// <param name="control"></param>
        /// <param name="style"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ForceSetStyle(this Control control, ControlStyles style, bool value)
        {
            Type typeTB = typeof(Control);
            System.Reflection.MethodInfo misSetStyle = typeTB.GetMethod("SetStyle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (misSetStyle != null && control != null)
            {
                misSetStyle.Invoke(control, new object[] { style, value });
                return true;
            }
            return false;
        }

        public static bool ForceMakeTransparent(this Control control, bool transparent = true)
        {
            return control.ForceSetStyle(ControlStyles.SupportsTransparentBackColor, transparent);
        }


        /// <summary>
        /// http://stackoverflow.com/questions/435433/what-is-the-preferred-way-to-find-focused-control-in-winforms-app
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static Control FindFocusedControl(this Control control)
        {
            var container = control as IContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }
            return control;
        }


        // http://stackoverflow.com/questions/4747935/c-sharp-winform-check-if-control-is-physicaly-visible
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT Point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y) { X = x; Y = y; }
            public static implicit operator Point(POINT p) => new Point(p.X, p.Y);
            public static implicit operator POINT(Point p) => new POINT(p.X, p.Y);
        }

        /// control 이 실제로 사용자에게 보이는지 여부를 반환
        public static bool IsControlVisibleToUser(this Control control)
        {
            return control.DoGet(() =>
            {
                if (!control.IsHandleCreated)
                    return false;

                var pos = control.PointToScreen(control.Location);
                var pointsToCheck = new POINT[] {
                                    pos,
                                    new Point(pos.X + control.Width - 1, pos.Y),
                                    new Point(pos.X, pos.Y + control.Height - 1),
                                    new Point(pos.X + control.Width - 1, pos.Y + control.Height - 1),
                                    new Point(pos.X + control.Width/2, pos.Y + control.Height/2),
                };

                foreach (var p in pointsToCheck)
                {
                    var hwnd = WindowFromPoint(p);
                    var other = Control.FromChildHandle(hwnd);
                    if (other == null)
                        continue;

                    if (control == other || control.Contains(other))
                        return true;
                }

                return false;
            });
        }


        #region Detect cursor
        //// http://stackoverflow.com/questions/586479/is-there-a-quick-way-to-get-the-control-thats-under-the-mouse
        //[DllImport("user32.dll")]
        //private static extern IntPtr WindowFromPoint(Point pnt);

        //public static Control ControlUnderPoint(this Point pt)
        //{
        //    IntPtr hWnd = WindowFromPoint(pt);
        //    if (hWnd != IntPtr.Zero)
        //        return Control.FromHandle(hWnd);

        //    return null;
        //}

        //public static Control ControlUnderMouseCursor()
        //{
        //    return Control.MousePosition.ControlUnderPoint();
        //}

        /// <summary> Checks whether mouse is over given control </summary>
        public static bool IsMouseOver(this Control control)
        {
            return control.ClientRectangle.Contains(control.PointToClient(Cursor.Position));
        }

        /// <summary> Collects controls under mouse cursor </summary>
        public static IEnumerable<Control> GetChildrenUnderMouse(this Control parent)
        {
            foreach (var c in parent.Controls.Cast<Control>())
            {
                if (c.IsMouseOver())
                    yield return c;
                foreach (var cc in GetChildrenUnderMouse(c))
                    yield return cc;
            }
        }
        #endregion


        /// Button 에 image 를 입힌다.  text 는 왼쪽, image 는 오른쪽
        public static void AddImage(this Button button, Image image)
        {
            button.Image = image;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                button.ImageAlign = ContentAlignment.MiddleRight;
                button.TextAlign = ContentAlignment.MiddleLeft;
            }
        }
    }
}
