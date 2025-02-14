using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Disposables;
using System.Collections.Concurrent;

/*
 * 기존 사용하던 CDiese 대용.  (https://www.codeproject.com/Articles/1916/ActionLists-for-Windows-Forms)
 * - CDiese 의 경우, DotNetCore 로 build 도 안되고, Net48 로 build 해도 NetCore 에서 참조시 오류 발생함.
 * - 아래 사용법 참고
 */

namespace Dual.Common.Winform
{
    /*
     * 사용 예
        - MainForm 이나 MainControl class 에 다음을 추가
            public partial class MainForm: ..
            {
                UiUpdator _uiUpdator = new UiUpdator();
                public MainForm() {
			        InitializeComponent();
                    _uiUpdator.StartMainLoop(this, updateUI);
		        }
                void updateUI()
                {
                    btnOk.Enabled = ...
		        }
	        }
        - SubForm 이나 control 들을 관리
		    public partial class SubForm : ..
		    {
			    UiUpdator _uiUpdator;
			    public SubForm(UiUpdator uiUpdator)
			    {
				    InitializeComponent();
				    IDisposable disposable = _uiUpdator.AddUiUpdatee(this, updateUI);
				    FormClosed += (s, e) => disposable.Dispose();
                    // or contrl 인 경우, HandleDestroyed 사용
			    }
                void updateUI()
                {
                    btnXX.Enabled = ...
		        }
		    }
     */

    public class UiUpdator
    {
        /// <summary>
        /// Thread 에서 update 관리해야할 control 및 action 쌍들
        /// </summary>
        protected ConcurrentDictionary<Control, Action> _updatees = new ();

        /// <summary>
        /// Update 가 필요한 control 및 action 등록.  Disposable dispose 시, 해당 control 의 update 가 중단된다.
        /// </summary>
        public IDisposable AddUiUpdatee(Control control, Action action)
        {
            _updatees.TryAdd(control, action);

            return Disposable.Create(() => _updatees.TryRemove(control, out Action action));
        }

        CancellationTokenSource startLoop(TimeSpan checkInterval, bool onlyUpdateOnActive)
        {
            CancellationTokenSource cts = new();
            Task.Factory.StartNew(async () => {
                while (!cts.IsCancellationRequested)
                {
                    List<Control> disposedControls = new List<Control>();
                    foreach (var kv in _updatees)
                    {
                        var (ctrl, act) = (kv.Key, kv.Value);
                        if (ctrl == null || ! ctrl.IsHandleCreated)
                        {
                            Trace.WriteLine($"::::::: INVALID CONTROL STATUS: {ctrl.Name}");
                            continue;
                        }

                        if (!ctrl.Visible)
                            continue;

                        if (ctrl.IsDisposed)
                        {
                            Trace.WriteLine($"::::::: CONTROL DISPOSED!: {ctrl.Name}");
                            return;
                        }

                        await ctrl.DoAsync(async tcs =>
                        {
                            try
                            {
                                await Task.Delay(checkInterval);
                                if (ctrl.IsDisposed)
                                    disposedControls.Add(ctrl);

                                else if (!onlyUpdateOnActive || ctrl.IsPartOfActiveProgram())
                                {
                                    // 활성 상태일 때에만 update 한다.
                                    act.Invoke();
                                }

                                tcs.SetResult(true);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"ERROR: {ex}");    // 크로스 스레드 작업이 잘못되었습니다. 'tbMessage' 컨트롤이 자신이 만들어진 스레드가 아닌 스레드에서 액세스되었습니다.
                            }
                        });
                    }
                    disposedControls.ForEach(ctrl => _updatees.TryRemove(ctrl, out Action action));
                }
            }, cts.Token);
            return cts;
        }

        /// <summary>
        /// Main control 의 update action 을 추가하고, thread loop 을 시작
        /// </summary>
        public virtual IDisposable StartMainLoop(Control mainControl, Action mainUpdateUIAction, TimeSpan checkInterval=default, bool onlyUpdateOnActive=true)
        {
            checkInterval = checkInterval == default ? TimeSpan.FromMilliseconds(300) : checkInterval; // 기본값 지정
            void doRegister()
            {
                var cts = startLoop(checkInterval, onlyUpdateOnActive);      // action, checkInterval
                mainControl.HandleDestroyed += (s, e) =>
                {
                    cts.Cancel();
                };
            }

            // main control 의 update action 등록
            AddUiUpdatee(mainControl, mainUpdateUIAction);

            // main control 의 handle 이 생성될 때까지 기다린 후, thread loop 을 시작
            if (mainControl.IsHandleCreated)
                doRegister();
            else
                mainControl.HandleCreated += (s, e) => doRegister();

            return Disposable.Create(() => _updatees.TryRemove(mainControl, out Action action));
        }
    }


    public static partial class EmControl
    {
        // { button 활성화시 색상 변경: Simple code
        public static Color EnabledColor { get; set; } = Color.LightCyan;
        public static Color DisabledColor { get; set; } = Color.LightGray;

        /// <summary>
        /// Control's SetEnabled
        /// </summary>
        public static void SetEnabled(this Control control, bool enable)
        {
            control.Enabled = enable;
            control.BackColor = enable ? EnabledColor : DisabledColor;
        }

        /// <summary>
        /// Control's Enable
        /// </summary>
        public static void Enable(this Control control) => control.SetEnabled(true);
        /// <summary>
        /// Control's Disable
        /// </summary>
        public static void Disable(this Control control) => control.SetEnabled(false);
        // } button 활성화시 색상 변경
    }

    /*
     * 사용 예
     *
    public partial class FormDebug : DevExpress.XtraEditors.XtraForm
    {
        UiUpdator _uiUpdator = new UiUpdator();
        public FormDebug()
        {
            InitializeComponent();

            IDisposable disposable = _uiUpdator.StartMainLoop(this, updateUI);
            this.Disposed += (s, e) => disposable.Dispose();
        }

        void updateUI()
        {
            btnXXX.Enabled = ...;
        }
    }

     *
     */
}

