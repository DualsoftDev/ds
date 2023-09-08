using DevExpress.XtraEditors;
using System.Media;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class MBox
{
    public static DialogResult Error(string text, string caption = "ERROR")
    {
        //Console.Beep();
        //SystemSounds.Hand.Play();
        SystemSounds.Beep.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    public static DialogResult AskYesNo(string text, string caption = "ANSWER")
    {
        SystemSounds.Hand.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }
    public static DialogResult WarningAskYesNo(string text, string caption = "ANSWER")
    {
        SystemSounds.Hand.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
    }
    public static DialogResult AskYesNoCancel(string text, string caption = "ANSWER")
    {
        SystemSounds.Hand.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
    }
    public static DialogResult Info(string text, string caption = "INFO")
    {
        SystemSounds.Hand.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    public static DialogResult Warn(string text, string caption = "Warning")
    {
        SystemSounds.Beep.Play();
        return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
