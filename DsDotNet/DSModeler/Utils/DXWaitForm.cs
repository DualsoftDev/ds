// https://www.devexpress.com/Support/Center/Question/Details/Q390152

using DevExpress.XtraWaitForm;
using System.Runtime.InteropServices;
using System.Threading;

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    [ComVisible(false)]
    public partial class DXWaitForm : WaitForm, IFormProgressbar
    {
        public static DXWaitForm HotDXForm;
        public DXWaitForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            progressPanel1.AutoHeight = true;
            HotDXForm = this;
        }

        #region Overrides

        public override void SetCaption(string caption)
        {
            this.Do(() =>
            {
                base.SetCaption(caption);
                progressPanel1.Caption = caption;
            });
        }

        public override void SetDescription(string description)
        {
            _descriptionSkeleton = description;
            this.Do(() =>
            {
                base.SetDescription(description);
                progressPanel1.Description = description;
            });
        }
        #endregion

        private string GetRealPercentageString()
        {
            return string.Format("{0:0.##}%", _portion * 100.0 / _total);
        }
        private void UpdateProgress()
        {
            this.Do(() =>
            {
                progressPanel1.Description = string.Format("{0} {1}", _descriptionSkeleton, GetRealPercentageString());
            });
        }

        private string _descriptionSkeleton;


        public string ProgressCaption { get => progressPanel1.Caption; set => SetCaption(value); }
        public string ProgressDescription { get => progressPanel1.Description; set => SetDescription(value); }
        public CancellationToken CancellationToken { get; private set; }
        public int ProgressTotal { get => _total; set { _total = value; _portion = 0; } }
        public int ProgressPortion
        {
            get => _portion;
            set
            {
                _portion = value;
                UpdateProgress();
            }
        }

        private int _total = 0;
        private int _portion = 0;

        public void AddProgressPortion(int portion)
        {
            _portion += portion;
            UpdateProgress();
        }

        public void StartProgressbar()
        {
            if (progressPanel1.Visible)
            {
                throw new Exception("Exclusive progress bar accessed simultaneously.");
            }

            progressPanel1.Visible = true;
            progressPanel1.BringToFront();
        }

        public void FinishProgressbar()
        {
            this.Do(() => { progressPanel1.Visible = false; });
        }


    }


    public interface IFormProgressbar
    {
        string ProgressCaption { get; set; }
        string ProgressDescription { get; set; }

        int ProgressTotal { get; set; }
        int ProgressPortion { get; set; }

        void AddProgressPortion(int portion);

        void StartProgressbar();
        void FinishProgressbar();
    }

}