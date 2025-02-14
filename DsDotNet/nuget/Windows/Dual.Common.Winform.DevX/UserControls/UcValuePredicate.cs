using System;
using System.Data;
using System.Linq;

using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;

using Dual.Common.Base.FS;
using static Dual.Common.Core.FS.ObjectHolderTypeModule;
using static PptToDs.FS.PptShapePropertiesModule;

namespace Dual.Common.Winform.DevX.UserControls
{
    public partial class UcValuePredicate : DevExpress.XtraEditors.XtraUserControl
    {
        public ValuePredicate ValuePredicate { get; private set; }
        public event EventHandler<ValuePredicate> ValueChanged;
        public string TextEditTooltip { get => tbExpression.ToolTip; set => tbExpression.ToolTip = value; }
        public string ComboTooltip { get => comboDataTypeSelector.ToolTip; set => comboDataTypeSelector.ToolTip = value; }

        public TextEdit ExpressionEditor => tbExpression;
        public ComboBoxEdit TypeCombo => comboDataTypeSelector;

        public UcValuePredicate()
        {
            InitializeComponent();
        }

        public void Initialize(ObjectHolderType[] allowedTypes, ObjectHolderType initialTypeSelection, string initialExpression="x == true")
        {
            var types = allowedTypes.Select(t => t.Stringify()).ToArray();
            comboDataTypeSelector.Properties.Items.AddRange(types);
            comboDataTypeSelector.SelectedIndex = types.FindIndex(typ => typ == initialTypeSelection.Stringify());

            tbExpression.Text = initialExpression;
        }

        private void UcValuePredicate_Load(object sender, EventArgs e)
        {
            // 타입 변경 시 현재 TextEdit 값을 새로운 타입으로 변환 시도
            comboDataTypeSelector.SelectedIndexChanged += (s, e) => handleChange();
            // TextEdit에 입력된 값이 변경될 때마다 변환 시도
            tbExpression.EditValueChanged += (s, e) => handleChange();
        }

        void handleChange()
        {
            var optTyp = ObjectHolderType.TryParse(comboDataTypeSelector.SelectedItem.ToString());
            var prev = ValuePredicate;
            ValuePredicate =
                optTyp.Bind(typ => ValuePredicate.TryParseSimple(typ, tbExpression.Text))
                    .MatchMap(
                        vp => vp,
                        () => throw new Exception("ERROR: Failed to convert")
                    );
            if (prev != ValuePredicate)
                ValueChanged?.Invoke(this, ValuePredicate);
        }
    }
}
