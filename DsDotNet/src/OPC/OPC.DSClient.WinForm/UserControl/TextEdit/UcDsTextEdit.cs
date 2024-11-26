using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Drawing;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTextEdit : XtraUserControl
    {
        public UcDsTextEdit()
        {
            InitializeComponent();
            InitializeRichEditControl();

            // 테마 변경 이벤트 핸들러 등록
            UserLookAndFeel.Default.StyleChanged += OnStyleChanged;

            // 초기 색상 설정
            ApplySkinColors();
        }

        private void InitializeRichEditControl()
        {
            richEditControl1.ReadOnly = true;
            richEditControl1.Options.HorizontalRuler.Visibility = RichEditRulerVisibility.Hidden; // Ruler 숨김
            richEditControl1.ActiveViewType = RichEditViewType.Simple; // 간소화된 보기
            richEditControl1.Appearance.Text.Font = new Font("Consolas", 12); // Monospace 폰트 적용
            richEditControl1.Text = ""; // 초기화
        }

        private void OnStyleChanged(object sender, EventArgs e)
        {
            ApplySkinColors();
        }

        private void ApplySkinColors()
        {
            var skin = CommonSkins.GetSkin(UserLookAndFeel.Default);

            if (skin != null)
            {
                // 컨트롤 배경색
                Color backgroundColor = skin.Colors["Control"];
                richEditControl1.BackColor = backgroundColor;

                // 컨트롤 글자색
                Color textColor = skin.Colors["ControlText"];
                richEditControl1.ForeColor = textColor;

                // 문서(종이) 영역 배경색
                Color documentBackgroundColor = skin.Colors["Window"];
                richEditControl1.ActiveView.BackColor = documentBackgroundColor;

                // 문서 영역 글자색 (기본 글자색 변경)
                richEditControl1.Document.DefaultCharacterProperties.ForeColor = textColor;
            }
        }

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            try
            {
                richEditControl1.Text = opcTagManager.OpcDsText;
                ApplySyntaxHighlighting(); // 텍스트 설정 후 하이라이트 적용
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error loading data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySyntaxHighlighting()
        {
            Document document = richEditControl1.Document;

            // 정규식으로 [ ] 패턴의 단어 및 // 주석 검색
            var regex = new System.Text.RegularExpressions.Regex(@"(\[[^\]]+\])|(//.*?$)", System.Text.RegularExpressions.RegexOptions.Multiline);

            // RichEditControl의 FindAll 메서드를 사용하여 모든 매칭 범위를 찾음
            DocumentRange[] searchResults = document.FindAll(regex);

            // [ ] 패턴과 // 주석에 대한 색상 정의
            var colors = new Dictionary<string, (Color backgroundColor, Color textColor)>
            {
                { "[sys]", (Color.LightBlue, Color.DarkBlue) },
                { "[flow]", (Color.LightGreen, Color.DarkGreen) },
                { "[job]", (Color.LightYellow, Color.DarkOrange) },
                { "[aliases]", (Color.LightPink, Color.DarkRed) },
                { "[buttons]", (Color.LightSteelBlue, Color.DarkSlateBlue) },
                { "[lamps]", (Color.LightPink, Color.DarkViolet) },
                { "[prop]", (Color.Lavender, Color.Indigo) },
                { "comment", (Color.Transparent, Color.Green) } // 주석은 녹색 글자로 표시
            };

            // 검색된 단어에 색상 적용
            foreach (var range in searchResults)
            {
                string text = document.GetText(range);

                // 주석 처리
                if (text.StartsWith("//"))
                {
                    var (backgroundColor, textColor) = colors["comment"];
                    CharacterProperties cp = document.BeginUpdateCharacters(range);
                    cp.BackColor = backgroundColor;
                    cp.ForeColor = textColor;
                    document.EndUpdateCharacters(cp);
                    continue;
                }

                // [ ] 패턴 색상 처리
                var (bgColor, fgColor) = colors.ContainsKey(text)
                    ? colors[text]
                    : (Color.LightGray, Color.Black); // 기본 색상

                CharacterProperties cpTag = document.BeginUpdateCharacters(range);
                cpTag.BackColor = bgColor;
                cpTag.ForeColor = fgColor;
                document.EndUpdateCharacters(cpTag);
            }
        }
    }
}
