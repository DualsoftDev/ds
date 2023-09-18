using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;

namespace Server.HW.Common
{
    public  class TagHW : ITagHW
    {
        public virtual TagIOType IOType { get; set; }
        public virtual TagDataType DataType { get; set; }
        [Browsable(false)]
        public ConnectionBase ConnectionBase { get; private set; }

        /// <summary> Tag parsing tokens </summary>
        protected virtual string[] Tokens { get; set; }
        [Browsable(false)]
        public object OldValue { get; protected set; }
        private object _value;

        /// <summary>
        /// Tag 의 값.  최초 생성 시, connection 을 통해 값을 받아 오기 전에는 null 값을 갖는다.
        /// HW 에 값을 write 요청할 때에는 WriteRequestValue  값을 설정한다.
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
        /// HW 에 쓰기 요청하는 값.  Value 에 직접 write 하게 되면 HW 에서 읽은 값과 혼동이 온다.
        [Browsable(false)]
        /// </summary>
        public object WriteRequestValue { get; set; }

        public virtual string Name { get; set; }
        [Browsable(false)]
        public virtual int BitOffset { get; protected set; }
        public string Address { get; private set; } = string.Empty;


        public object DefaultValue
        {
            get
            {
                switch (DataType)
                {
                    case TagDataType.Bool: return false;
                    case TagDataType.Single: return 0.0f;
                    case TagDataType.Double: return 0.0;
                    case TagDataType.Int16 : return (short)0;
                    case TagDataType.Int32 : return 0;
                    case TagDataType.Int64 : return 0L;
                    case TagDataType.Sbyte: return (sbyte)0;
                    case TagDataType.String: return "";
                    case TagDataType.Uint16: return (ushort)0;
                    case TagDataType.Uint32: return 0U;
                    case TagDataType.Uint64: return 0UL;
                    case TagDataType.Byte: return (byte)0;
                    default: throw new Exception("Unsupported type.");
                }
            }
        }


               

        /// <summary>
        /// Melsec, Fuji 등의 HW 에 대해서 tag 의 byte offset 을 반환한다.  AB 의 경우처럼 환산 불가면 null
        /// e.g
        /// Melsec
        ///     X23 -> 0x23 = 35-th bit -> 5-th byte -> 5
        /// </summary>
        [Browsable(false)]
        public virtual int ByteOffset { get; protected set; }
        [Browsable(false)]

        public int NumDots => Name.Count(c => c == '.');
        [Browsable(false)]

        public virtual bool IsBitAddress { get { return DataType == TagDataType.Bool; } }

        public TagHW(ConnectionBase connection) { ConnectionBase = connection; }
        public void SetAddress(string name, int bitOffset, TagIOType tagIOType)
        {
            var upperName = name.ToUpper().Trim();
            Address = upperName;
            IOType = tagIOType;
            ByteOffset = bitOffset / 8;
            BitOffset = bitOffset;
        }
     
    }
}
