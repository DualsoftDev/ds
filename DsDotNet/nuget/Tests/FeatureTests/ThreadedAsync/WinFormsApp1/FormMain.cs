using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Dual.Common.Winform;

namespace WinFormsApp1
{
    public partial class FormMain : Form
    {
        internal int Count = 0;
        public int ThreadId { get; private set; }
        public FormMain()
        {
            InitializeComponent();
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Application.Idle += (s, e) => textBoxState.Text = $"{Count++}";
        }

        ThreadedForm _threadForm;
        private async void btnCreateThreadedForm_Click(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                Trace.WriteLine($"Creating form on thread {id}");
                _threadForm = new ThreadedForm();
                Debug.Assert(_threadForm.ThreadId == id);
                _threadForm.ShowDialog();
                Trace.WriteLine($"Done on thread {id}");
            });


            //await this.DoAsync(async tcs =>
            //{
            //    this.Count = 0;
            //    await Task.Delay(1000);
            //    this.Text = Count.ToString();
            //    tcs.SetResult(true);
            //});
        }

        private async void btnInvoke_Click(object sender, EventArgs e)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == this.ThreadId);
            btnInvoke.Enabled = false;
            var x = await _threadForm.DoAsyncT<bool>(async (tcs) =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadForm.ThreadId);
                Trace.WriteLine($"{DateTime.Now} : Executing on thread {Thread.CurrentThread.ManagedThreadId}");
                _threadForm.SetText("");
                _threadForm.Count = 0;
                // 비동기 작업 수행 (2초 동안 대기하는 비동기 작업 예시)
                await Task.Delay(2000);
                await _threadForm.SetTextAsync("Hello");

                Console.WriteLine("Async Thread: Async operation completed.");
                Trace.WriteLine($"{DateTime.Now} : Finished on thread {Thread.CurrentThread.ManagedThreadId}");
                tcs.SetResult(false);
            });

            btnInvoke.Enabled = true;
            Count = 0;
            Trace.WriteLine($"{DateTime.Now} : Finished invoking on thread {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
