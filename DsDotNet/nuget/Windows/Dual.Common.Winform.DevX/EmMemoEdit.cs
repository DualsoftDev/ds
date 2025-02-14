using System;
using System.Collections.Generic;
using System.Windows.Forms;

using DevExpress.XtraEditors;

namespace Dual.Common.Winform
{
    public static class EmMemoEdit
    {
        #region MemoEdit_Auto_ScrollBar
        /* MemoEdit 컨트롤의 텍스트 높이가 컨트롤 높이를 초과할 때만 우측 스크롤바 표시 */
        static HashSet<MemoEdit> _memeEdits = new HashSet<MemoEdit>();
        public static void UpdateScrollBars(this MemoEdit memoEdit)
        {
            // 텍스트 높이를 계산
            var textSize = memoEdit.CreateGraphics().MeasureString(
                memoEdit.Text, memoEdit.Font, memoEdit.Width).Height;

            // 텍스트 높이가 컨트롤 높이를 초과할 때만 스크롤바 표시
            if (textSize > memoEdit.ClientSize.Height)
                memoEdit.Properties.ScrollBars = ScrollBars.Vertical;
            else
                memoEdit.Properties.ScrollBars = ScrollBars.None;
        }

        private static void MemoEdit_SizeChanged(object sender, EventArgs e)
        {
            (sender as MemoEdit).UpdateScrollBars();
        }

        /// <summary>
        /// MemoEdit 컨트롤의 텍스트를 설정하고 스크롤바를 업데이트한다.
        /// MemoEdit resize 시, 자동으로 scrollbar 를 업데이트한다.
        /// </summary>
        /// <param name="memoEdit"></param>
        /// <param name="text"></param>
        public static void SetText(this MemoEdit memoEdit, string text)
        {
            if (!_memeEdits.Contains(memoEdit))
            {
                memoEdit.Disposed += (s, e) =>
                {
                    memoEdit.SizeChanged -= MemoEdit_SizeChanged;
                    _memeEdits.Remove(memoEdit);
                };
                memoEdit.SizeChanged += MemoEdit_SizeChanged;
                _memeEdits.Add(memoEdit);
            }

            memoEdit.Text = text;
            memoEdit.UpdateScrollBars();
        }
        #endregion  // MemoEdit_Auto_ScrollBar


    }
}
