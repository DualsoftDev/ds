using Dual.Common.Core;
using Dual.Common.Winform;

using Dual.Common.Core.FS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Engine.Core;
using static Dual.Common.Core.FS.MessageEvent;
using Microsoft.FSharp.Core;
using System.Runtime.CompilerServices;

namespace Dual.Model.Import
{
    public static class UtilFile
    {
        public static string GetNewPathXls(List<string> pptPaths)
        {
            var newPath = Path.Combine(Path.GetDirectoryName(pptPaths.First())
                        , string.Join("_", pptPaths.Select(s => Path.GetFileNameWithoutExtension(s))));

            var excelName = Path.GetFileNameWithoutExtension(newPath) + $"_{DateTime.Now.ToString("yyMMdd(HH-mm-ss)")}.xlsx";
            var excelDirectory = Path.Combine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(excelName));
            Directory.CreateDirectory(excelDirectory);

            return Path.Combine(excelDirectory, excelName);
        }


        public static string GetVersion()
        {
            var text = "";

            StreamReader sr = new StreamReader($"{Application.StartupPath}\\last-commit");
            while (sr.Peek() >= 0)
            {
                var txt = sr.ReadLine();
                if (txt.Split('|').Length > 1)
                    text = string.Join(", ", txt.Split('|').Skip(1));
                break;
            }
            sr.Close();

            return text;
        }

        public static bool BusyCheck()
        {
            if (DsProcessEvent.CurrProcess != 0)
            {
                MessageBox.Show("프로세스 처리 작업중입니다.", "Model.Import.Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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


}

