using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ThreadedForm : Form
    {
        internal int Count = 0;
        public int ThreadId { get; private set; }

        public ThreadedForm()
        {
            InitializeComponent();
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Application.Idle += (s, e) => textBoxState.Text = $"{Count++}";
        }

        public void SetText(string text)
        {
            textBox1.Text = text;
        }
        public async Task SetTextAsync(string text)
        {
            await Task.Delay(3000);
            textBox1.Text = text;
        }
    }
}
