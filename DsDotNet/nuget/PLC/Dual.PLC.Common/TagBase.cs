using System;
using System.Linq;
using LanguageExt;

namespace Dual.PLC.Common
{
    public enum DataLengthType
    {
        Undefined = 0,
        /// 1 Bit
        Bit,
        /// 1 Byte
        Byte,
        /// 2 Byte
        Word,
        /// 4 Byte
        DWord,
        /// 8 Byte
        LWord,
    }

    public abstract class TagBase : ITag
    {
        public ConnectionBase ConnectionBase { get; private set; }

        /// <summary> Tag parsing tokens </summary>
        protected virtual string[] Tokens { get; set; }

        public object OldValue { get; protected set; }
        private object _value;

        /// <summary>
        /// 사용자가 임의로 설정할 수 있는 값.
        /// </summary>
        public object UserObject { get; set; }

        /// <summary>
        /// Tag 의 값.  최초 생성 시, connection 을 통해 값을 받아 오기 전에는 null 값을 갖는다.
        /// PLC 에 값을 write 요청할 때에는 WriteRequestValue  값을 설정한다.
        /// </summary>
        public virtual object Value
        {
            get { return _value; }
            set
            {
                if (!Object.Equals(_value, value))
                {
                    OldValue = _value;
                    _value = value;
                    ConnectionBase.Subject.OnNext(new TagValueChangedEvent(this));
                }
            }
        }

        /// <summary>
        /// PLC 에 쓰기 요청하는 값.  Value 에 직접 write 하게 되면 PLC 에서 읽은 값과 혼동이 온다.
        /// </summary>
        public object WriteRequestValue { internal get; set; }

        public virtual DataLengthType DataLengthType { get; }
        public virtual string Name { get; set; }
        public virtual TagType Type { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Option<int> BitOffset { get; protected set; }

        /// <summary>
        /// Melsec, Fuji 등의 PLC 에 대해서 tag 의 byte offset 을 반환한다.  AB 의 경우처럼 환산 불가면 null
        /// e.g
        /// Melsec
        ///     X23 -> 0x23 = 35-th bit -> 5-th byte -> 5
        /// </summary>
        public virtual Option<int> ByteOffset { get; protected set; }
        public int NumDots => Name.Count(c => c == '.');
        public virtual bool IsBitAddress { get { return Type == TagType.Bit; } }

        public TagBase(ConnectionBase connection) { ConnectionBase = connection; }
    }
}
