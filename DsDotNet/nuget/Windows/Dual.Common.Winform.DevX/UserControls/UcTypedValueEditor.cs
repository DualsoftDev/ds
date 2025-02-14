using System;
using System.Linq;

using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;


using Dual.Common.Base.FS;
using static Dual.Common.Core.FS.ObjectHolderTypeModule;

namespace Dual.Common.Winform.DevX
{
    public partial class UcTypedValueEditor : XtraUserControl
    {
        Action<Exception> onError = null;
        public ObjectHolder ObjectHolder { get; private set; }
        public object EditedValue => ObjectHolder?.Value; // 최종적으로 변환된 값
        public event EventHandler<ObjectHolder> ValueChanged;
        public string TextEditTooltip { get => textEditValue.ToolTip; set => textEditValue.ToolTip = value; }
        public string ComboTooltip { get => comboDataTypeSelector.ToolTip; set => comboDataTypeSelector.ToolTip = value; }

        public TextEdit ValueEditor => textEditValue;
        public ComboBoxEdit TypeCombo => comboDataTypeSelector;

        public UcTypedValueEditor()
        {
            InitializeComponent();
        }

        public void Initialize(ObjectHolderType[] allowedTypes, ObjectHolderType initialSelection, object initialValue)
        {
            var types = allowedTypes.Select(t => t.Stringify()).ToArray();
            comboDataTypeSelector.Properties.Items.AddRange(types);
            comboDataTypeSelector.SelectedIndex = types.FindIndex(typ => typ == initialSelection.Stringify());
            textEditValue.Text = initialValue?.ToString();
        }

        private void UcTypedValueEditor_Load(object sender, EventArgs e)
        {
            // 타입 변경 시 현재 TextEdit 값을 새로운 타입으로 변환 시도
            comboDataTypeSelector.SelectedIndexChanged += (s, e) => ConvertTextValue();
            // TextEdit에 입력된 값이 변경될 때마다 변환 시도
            textEditValue.EditValueChanged += (s, e) => ConvertTextValue();
        }

        private void ConvertTextValue()
        {
            string inputValue = textEditValue.Text;
            var optTyp = ObjectHolderType.TryParse(comboDataTypeSelector.SelectedItem.ToString());
            var prev = ObjectHolder;
            ObjectHolder =
                optTyp
                    .MatchMap(
                        typ =>
                        {
                            ObjectHolder holder = typ.CreateObjectHolder(inputValue);
                            var val = holder.Value ?? typ.GetDefaultValue();

                            textEditValue.Text = val.ToString();
                            return holder;
                        },
                        () =>
                        {
                            onError?.Invoke(new Exception("ERROR: Failed to convert"));
                            return null;
                        }
                    );
            if ( ((prev == null) != (ObjectHolder == null))
                    || (prev.Type != ObjectHolder.Type) || (prev.Value != ObjectHolder.Value))
            {
                ValueChanged?.Invoke(this, ObjectHolder);
            }
        }
    }
}
