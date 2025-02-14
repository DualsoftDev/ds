using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Forms;

using DevExpress.XtraSplashScreen;

namespace Dual.Common.Winform
{
    public partial class DcWaitForm : DevExpress.XtraWaitForm.WaitForm
    {
        public DcWaitForm()
        {
            InitializeComponent();
            this.progressPanel1.AutoHeight = true;
            this.TopLevel = true;
            this.ShowOnTopMode = DevExpress.XtraWaitForm.ShowFormOnTopMode.AboveAll;
        }

        #region Overrides

        public override void SetCaption(string caption)
        {
            base.SetCaption(caption);
            this.progressPanel1.Caption = caption;
        }
        public override void SetDescription(string description)
        {
            base.SetDescription(description);
            this.progressPanel1.Description = description;
        }
        public override void ProcessCommand(Enum cmd, object arg)
        {
            base.ProcessCommand(cmd, arg);
        }

        #endregion



        /// Wait form 이 동시에 두번 호출되면 exception.  이를 회피하기 위함
        public static bool _waitFormCreated = false;
        static void closeWaitForm()
        {
            if (_waitFormCreated)
            {
                SplashScreenManager.CloseForm();
                _waitFormCreated = false;
            }
        }
        public static IDisposable CreateWaitForm(string caption = "Loading...", string description = "Please Wait...", Point? location = null, bool disposeOnOtherPopup=true)
        {
            var disposables = new CompositeDisposable();
            if (_waitFormCreated)
                return disposables;

            try
            {
                bool disposed = false;
                var disposable = Disposable.Create(() =>
                {
                    closeWaitForm();
                    disposed = true;
                });
                if (disposeOnOtherPopup)
                {
                    /* WaitForm 구동 중에 다른 form 이 popup 되는 경우, WaitForm 취소하기 위한 작업 */
                    /// 트래킹 중인 폼 목록
                    HashSet<Form> trackedForms = Application.OpenForms.Cast<Form>().ToHashSet();

                    // timer 를 이용해서 주기적으로 신규로 popup 된 form 이 존재하는 검사
                    EmWinformTimer.Do(timer => {
                        /// popup 이 wait form 자체 인 경우 제외하고, 신규로 popup 되는 form
                        var popupForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => !(f is DcWaitForm) && !trackedForms.Contains(f));
                        if (popupForm != null)
                        {
                            closeWaitForm();
                            timer.Stop();

                            // popup form 이 닫히면, 다시 wait form 을 띄운다.
                            popupForm.FormClosed += (s, e) =>
                            {
                                Trace.WriteLine($"Popup form ({popupForm.Text}) closed.");
                                if (!disposed)
                                    startWaitForm();
                            };
                        }
                    }, 200);
                }

                void startWaitForm()
                {
                    _waitFormCreated = true;
                    SplashScreenManager.ShowForm(typeof(DcWaitForm));
                    SplashScreenManager.Default.SetWaitFormCaption(caption);
                    SplashScreenManager.Default.SetWaitFormDescription(description);

                    SplashScreenManager.Default.SplashFormLocation = location ?? EmForm.GetScreenCenter();
                }

                startWaitForm();

                return disposable;

            }
            catch (Exception)
            {
                return Disposable.Create(() => {});
            }
        }

        public static void UpdateDescription(string description) =>
            SplashScreenManager.Default?.SetWaitFormDescription(description);
    }
}