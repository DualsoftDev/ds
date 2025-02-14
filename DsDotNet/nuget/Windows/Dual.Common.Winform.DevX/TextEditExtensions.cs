using System;
using System.ComponentModel;

using DevExpress.Utils;
using DevExpress.XtraEditors;

using Dual.Common.Base.CS;

namespace Dual.Common.Winform.DevX
{
    public static class TextEditExtensions
    {
        public static void MakeIntType(this TextEdit textEdit, Func<int, string> errorValidator=null)
        {
            void validating(object sender, CancelEventArgs e)
            {
                void cancel(string error)
                {
                    e.Cancel = true; // 유효하지 않은 경우 입력 취소
                    textEdit.ErrorText = error;
                }

                textEdit.ErrorText = ""; // 유효한 경우 오류 메시지 초기화
                if (string.IsNullOrEmpty(textEdit.Text))
                    cancel("이 필드는 필수입니다.");
                else if (!int.TryParse(textEdit.Text, out int value)) // 숫자만 허용하는 경우
                    cancel("숫자만 입력하세요.");
                else
                {
                    if (errorValidator != null)
                    {
                        string error = errorValidator.Invoke(value);
                        if (error.NonNullAny())
                            cancel(error);
                    }
                }
            }
            textEdit.Validating += new CancelEventHandler(validating);
        }

        public static void MakeDoubleType(this TextEdit textEdit, Func<double, string> errorValidator=null)
        {
            void validating(object sender, CancelEventArgs e)
            {
                void cancel(string error)
                {
                    e.Cancel = true; // 유효하지 않은 경우 입력 취소
                    textEdit.ErrorText = error;
                }

                textEdit.ErrorText = ""; // 유효한 경우 오류 메시지 초기화
                if (string.IsNullOrEmpty(textEdit.Text))
                    cancel("이 필드는 필수입니다.");
                else if (!double.TryParse(textEdit.Text, out double value)) // 숫자만 허용하는 경우
                    cancel("숫자만 입력하세요.");
                else
                {
                    if (errorValidator != null)
                    {
                        string error = errorValidator.Invoke(value);
                        if (error.NonNullAny())
                            cancel(error);
                    }
                }
            }
            textEdit.Validating += new CancelEventHandler(validating);
        }

    }
}
