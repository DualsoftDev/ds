using Engine.Common;
using Engine.Common.FS;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Model.Simulator
{
    public static class UtilFile
    {
        public static bool BusyCheck()
        {
            if (FormMain.TheMain.Busy)
            {
                MessageEvent.MSGWarn("작업중입니다.");
                return true;
            }
            return false;
        }

    }
    public static class RichTextBoxExtensions
    {
        public static void AppendTextColor(this RichTextBox box, string text, Color color)
        {
            FormMain.TheMain.Do(() =>
             {
                 box.SelectionStart = box.TextLength;
                 box.SelectionLength = 0;

                 box.SelectionColor = color;
                 box.AppendText(text);
                 box.SelectionColor = box.ForeColor;
             });

        }
        public static void SetClipboard(string value)
        {
            if (value == null)
                throw new ArgumentNullException("Attempt to set clipboard with null");

            Process clipboardExecutable = new Process();
            clipboardExecutable.StartInfo = new ProcessStartInfo // Creates the process
            {
                RedirectStandardInput = true,
                FileName = @"clip",
                UseShellExecute = false
            };
            clipboardExecutable.Start();

            clipboardExecutable.StandardInput.Write(value); // CLIP uses STDIN as input.
                                                            // When we are done writing all the string, close it so clip doesn't wait and get stuck
            clipboardExecutable.StandardInput.Close();

            return;
        }
    }

    public static class UtilUI
    {

    }

}

