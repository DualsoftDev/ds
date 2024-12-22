using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;

using StackExchange.Redis;

using Dual.Common.AppSettings;
using Dual.Common.Winform;

using static PptToDs.FS.Prelude.KeyMouseModule;

namespace GlobalEventDetector
{
    public partial class HookTestForm : Form
    {
        AppSettings _appSettings;
        MouseHook mouseHook = new MouseHook();
        KeyboardHook keyboardHook = new KeyboardHook();
        Subject<KeyMouseData> _keyMouseData = new Subject<KeyMouseData>();

        private bool throttled = false;
        private Timer throttleTimer = new Timer() { Interval = 100 };

        public HookTestForm()
        {
            InitializeComponent();
            throttleTimer.Tick += (s, e) => ResetThrottle();
        }
        void ResetThrottle()
        {
            throttled = false;
            throttleTimer.Stop();
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            _appSettings = JsonSetting.GetSectionEx<AppSettings>("appsettings.json", "AppSettings");
            var types = _appSettings.EventTypes.ToHashSet();

            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();
            var sub = redis.GetSubscriber();

            void publish(KeyMouseData data)
            {
                string channel = data.Type switch
                {
                    "MM" => "mouse",
                    "MD" => "mouse",
                    "MU" => "mouse",
                    "KD" => "key",
                    "KU" => "key",
                    "KP" => "key",
                    _ => throw new ArgumentException("Invalid type")
                };
                var str = data.Stringify();
                sub.Publish(channel, str);
                Debug.WriteLine($"Eject: {str}");
            }

            // MouseMove 이벤트에 대한 Throttle 적용
            _keyMouseData
                .Where(data => data.Type == "MM") // MM 이벤트만 처리
                .Throttle(TimeSpan.FromMilliseconds(_appSettings.EjectIntervalMs))
                .Subscribe(publish);

            // MouseDown, MouseUp 이벤트는 즉시 처리
            _keyMouseData
                .Where(data => data.Type != "MM") // MM이 아닌 이벤트
                .Subscribe(publish);

            //// 두 스트림을 병합
            ///*_subscription =*/
            //mouseMoveThrottled
            //    .Merge(immediateEvents)
            //    .Subscribe(data =>
            //    {
            //        var str = data.Stringify();
            //        sub.Publish(channel, str);
            //        Debug.WriteLine($"Eject: {str}");
            //    });



            //_keyMouseData
            //    .Throttle(TimeSpan.FromMilliseconds(_appSettings.EjectIntervalMs))
            //    .Subscribe(data =>
            //    {
            //        var str = data.Stringify();
            //        sub.Publish(channel, str);
            //        Debug.WriteLine($"Eject: {str}");
            //    });

            if (types.Contains("MM"))
                mouseHook.MouseMove += new MouseEventHandler(mouseHook_MouseMove);
            if (types.Contains("MD"))
                mouseHook.MouseDown += new MouseEventHandler(mouseHook_MouseDown);
            if (types.Contains("MU"))
                mouseHook.MouseUp += new MouseEventHandler(mouseHook_MouseUp);
            if (types.Contains("MW"))
                mouseHook.MouseWheel += new MouseEventHandler(mouseHook_MouseWheel);

            if (types.Contains("KD"))
                keyboardHook.KeyDown += new KeyEventHandler(keyboardHook_KeyDown);
            if (types.Contains("KU"))
                keyboardHook.KeyUp += new KeyEventHandler(keyboardHook_KeyUp);
            if (types.Contains("KP"))
                keyboardHook.KeyPress += new KeyPressEventHandler(keyboardHook_KeyPress);

            if (types.Intersect(new[] { "MM", "MD", "MU", "MW" }).Any())
                mouseHook.Start();
            if (types.Intersect(new[] { "KD", "KU", "KP" }).Any())
                keyboardHook.Start();

            SetXYLabel(MouseSimulator.X, MouseSimulator.Y);
        }

        void keyboardHook_KeyPress(object sender, KeyPressEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "KP"));
            AddKeyboardEvent(
                "KeyPress",
                "",
                e.KeyChar.ToString(),
                "",
                "",
                ""
                );
        }

        void keyboardHook_KeyUp(object sender, KeyEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "KU"));
            AddKeyboardEvent(
                "KeyUp",
                e.KeyCode.ToString(),
                "",
                e.Shift.ToString(),
                e.Alt.ToString(),
                e.Control.ToString()
                );
        }

        void keyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "KD"));
            AddKeyboardEvent(
                "KeyDown",
                e.KeyCode.ToString(),
                "",
                e.Shift.ToString(),
                e.Alt.ToString(),
                e.Control.ToString()
                );
        }

        void mouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "MW"));
            AddMouseEvent(
                "MouseWheel",
                "",
                "",
                "",
                e.Delta.ToString()
                );
        }

        void mouseHook_MouseUp(object sender, MouseEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "MU"));
            AddMouseEvent(
                "MouseUp",
                e.Button.ToString(),
                e.X.ToString(),
                e.Y.ToString(),
                ""
                );
        }

        void mouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "MD"));
            AddMouseEvent(
                "MouseDown",
                e.Button.ToString(),
                e.X.ToString(),
                e.Y.ToString(),
                ""
                );
        }


        void mouseHook_MouseMove(object sender, MouseEventArgs e)
        {
            if (!throttled)
            {
                throttled = true; // throttle 활성화
                _keyMouseData.OnNext(KeyMouseData.Create(sender, e, "MM"));

                // 타이머 시작
                throttleTimer.Start();
            }

            SetXYLabel(e.X, e.Y);
        }

        void SetXYLabel(int x, int y)
        {
            curXYLabel.Text = String.Format("Current Mouse Point: X={0}, y={1}", x, y);
        }

        void AddMouseEvent(string eventType, string button, string x, string y, string delta)
        {
            listView1.Items.Insert(0,
                new ListViewItem(
                    new string[]{
                        eventType,
                        button,
                        x,
                        y,
                        delta
                    }));
        }

        void AddKeyboardEvent(string eventType, string keyCode, string keyChar, string shift, string alt, string control)
        {
            listView2.Items.Insert(0,
                 new ListViewItem(
                     new string[]{
                        eventType,
                        keyCode,
                        keyChar,
                        shift,
                        alt,
                        control
                }));
        }

        private void TestForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Not necessary anymore, will stop when application exits
            mouseHook.Stop();
            keyboardHook.Stop();
        }
    }
}
