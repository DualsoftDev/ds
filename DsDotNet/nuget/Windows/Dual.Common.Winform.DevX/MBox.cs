using DevExpress.XtraEditors;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public static class G
    {
        public static Control Control => Application.OpenForms[0];

    }
    /// <summary>
    /// Info Message Box
    /// </summary>
    public static class MBoxExtensions
    {
        /// <summary>
        /// Ok
        /// </summary>
        public static DialogResult MBoxOK(this IWin32Window owner, string message, string title = "INFO") =>
            XtraMessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        /// <summary>
        /// Yes, No
        /// </summary>
        public static DialogResult MBoxYN(this IWin32Window owner, string message, string title = "Answer:") =>
            XtraMessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        /// <summary>
        /// Ok
        /// </summary>
        public static DialogResult MBoxErr(this IWin32Window owner, string message, string title = "Error:")
        {
            EmForm.PlaySystemWave();
            return XtraMessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    ///// <summary>
    ///// Query Message Box
    ///// </summary>
    //public static class MBoxQ
    //{
    //    /// <summary>
    //    /// Yes, No
    //    /// </summary>
    //    public static DialogResult YN(string message, string title = "Answer:") =>
    //        XtraMessageBox.Show(G.Control, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    //    /// <summary>
    //    /// Yes, No, Cancel
    //    /// </summary>
    //    public static DialogResult YNC(string message, string title = "Answer:") =>
    //        XtraMessageBox.Show(G.Control, message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
    //}


    ///// <summary>
    ///// Warning Message Box
    ///// </summary>
    //public static class MBoxW
    //{
    //    /// <summary>
    //    /// Yes, No
    //    /// </summary>
    //    public static DialogResult YN() { return DialogResult.Yes; }
    //    /// <summary>
    //    /// Yes, No, Cancel
    //    /// </summary>
    //    public static DialogResult YNC() { return DialogResult.Yes; }
    //    /// <summary>
    //    /// Ok
    //    /// </summary>
    //    public static DialogResult O() { return DialogResult.OK; }
    //}

    ///// <summary>
    ///// Error Message Box
    ///// </summary>
    //public static class MBoxE
    //{
    //    /// <summary>
    //    /// Yes, No
    //    /// </summary>
    //    public static DialogResult YN() { return DialogResult.Yes; }
    //    /// <summary>
    //    /// Yes, No, Cancel
    //    /// </summary>
    //    public static DialogResult YNC() { return DialogResult.Yes; }
    //    /// <summary>
    //    /// Ok
    //    /// </summary>
    //    public static DialogResult O() { return DialogResult.OK; }
    //}
}
