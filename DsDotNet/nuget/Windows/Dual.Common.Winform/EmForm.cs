using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static class EmForm
    {
        static void markAcceptCancelButtons(this Form form, IButtonControl btnOK, IButtonControl btnCacnel)
        {
            form.AcceptButton = btnOK;
            form.CancelButton = btnCacnel;
        }
        static void anchorOKCancelButtons(Control btnOK, Control btnCacnel)
        {
            btnOK.Anchor = btnCacnel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        }

        /// <summary>
        /// Form 의 OK, Cancel 버튼을 설정한다.
        /// <br/> - OK 버튼을 누르면 validator 가 있으면 validator() 가 true 일 때만 DialogResult.OK 를 설정한다.
        /// <br/> - Accept/Cancel 버튼을 설정한다.
        /// <br/> - anchorBottomRight 가 true 인 경우, OK, Cancel 버튼의 anchor 를 우하단으로 설정한다.
        /// </summary>
        public static void MakeOKCancel(this Form form, Control btnOK, Control btnCacnel, Func<bool> validator=null, bool anchorBottomRight=false)
        {
            if (btnOK is IButtonControl == false || btnCacnel is IButtonControl == false)
                throw new ArgumentException("btnOK, btnCacnel must be IButtonControl");

            form.markAcceptCancelButtons((IButtonControl)btnOK, (IButtonControl)btnCacnel);
            if (anchorBottomRight )
                anchorOKCancelButtons(btnOK, btnCacnel);

            btnOK.Click += (s, e) =>
            {
                if (validator != null && !validator())
                    return;

                form.DialogResult = DialogResult.OK;
            };
            btnCacnel.Click += (s, e) => form.DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// 현재 mouse cursor 위치가 존재하는 screen 기준으로 화면 중앙 위치를 반환한다.
        /// </summary>
        public static Point GetScreenCenter()
        {
            // 현재 화면 (주로 마우스 또는 활성 폼 기준)
            Screen currentScreen = Screen.FromPoint(Cursor.Position);

            // 화면 작업 영역의 중앙 계산
            int centerX = currentScreen.WorkingArea.Left + (currentScreen.WorkingArea.Width / 2);
            int centerY = currentScreen.WorkingArea.Top + (currentScreen.WorkingArea.Height / 2);

            return new Point(centerX, centerY);
        }

        private static HashSet<Form> _formsCentered = new HashSet<Form>();
        /// <summary>
        /// Form 을 화면 중앙에 위치시킨다.
        /// </summary>
        public static Form PlaceAtScreenCenter(this Form form)
        {
            var (w, h) = (form.Width, form.Height);
            var center = GetScreenCenter();
            var location = new Point(center.X - w / 2, center.Y - h / 2);
            form.Location = location;

            if (! _formsCentered.Contains(form))
            {
                _formsCentered.Add(form);
                // form 의 InitializeComponent() 이후에 location 이 변경되는 경우가 있으므로 load 후 한번 더 수행되도록 강제..
                form.Load += (s, e) => form.Location = location;
            }

            return form;
        }



        private static HashSet<Form> _formsBeeped = new HashSet<Form>();
        /// <summary>
        /// Form Loading 시 beep 음
        /// </summary>
        public static Form BeepOnLoad(this Form form, string systemWaveFile="Windows Ding.wav")
        {
            if (!_formsBeeped.Contains(form))
            {
                _formsBeeped.Add(form);
                form.Load += (s, e) => PlaySystemWave(systemWaveFile);
            }
            return form;
        }

        // todo : 적절한 위치로 이동 필요
        /// <summary>
        /// C:\Windows\Media\{mediaWaveFileName} 음원을 재생한다.  mediaWaveFileName default = "Windows Error.wav"
        /// </summary>
        /// <param name="mediaWaveFileName"></param>
        public static void PlaySystemWave(string mediaWaveFileName= "Windows Critical Stop.wav")
        {
            string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string mediaPath = Path.Combine(windowsDir, "Media", mediaWaveFileName);
            using (var player = new SoundPlayer(mediaPath))
                player.Play();
        }
        public static Form PlaySystemWaveOnLoad(this Form form, string mediaWaveFileName= "Windows Critical Stop.wav")
        {
            if (!_formsBeeped.Contains(form))
            {
                _formsBeeped.Add(form);
                form.Load += (s, e) => PlaySystemWave(mediaWaveFileName);
            }
            return form;
        }

    }
}
