using System;
using System.Reactive.Subjects;
using XGCommLib;

namespace XGTComm
{
    /// <summary>
    /// Represents a generic XGT device with common properties and methods.
    /// This abstract class is the base for specific types of XGT devices.
    /// </summary>
    public abstract class XGTDevice
    {
        public bool InitUpdated { get; set; }
        protected Subject<XGTDevice> ChangeValueSubject = new Subject<XGTDevice>();

        public enum XGTDeviceSize
        {
            Bit,    ///< [0 ~ 7]
            Byte,   ///< 1 byte
            Word,   ///< 2 bytes
            DWord,  ///< 4 bytes
            LWord   ///< 8 bytes
        }

        private XGTDeviceSize _deviceSize;
        private int _offsetBit;
        private char _device;

        // Method to subscribe to value changes
        public IDisposable SubscribeToChanges(Action<XGTDevice> onNext)
        {
            return ChangeValueSubject.Subscribe(onNext);
        }
        public XGTDevice(char device, XGTDeviceSize deviceSize, int offsetBit)
        {
            _device = device;
            _deviceSize = deviceSize;
            _offsetBit = offsetBit;
        }


        public char Device => _device;
        public string ToText() => $"{_device}{_deviceSize}{_offsetBit}";


        public char MemType => _deviceSize == XGTDeviceSize.Bit ? 'X'
                             : _deviceSize == XGTDeviceSize.Byte ? 'B'
                             : _deviceSize == XGTDeviceSize.Word ? 'B'
                             : _deviceSize == XGTDeviceSize.DWord ? 'B'
                             : _deviceSize == XGTDeviceSize.LWord ? 'B'
                             : throw new Exception($"Size value error : current {_deviceSize}");


        public int Size => _deviceSize == XGTDeviceSize.Bit ? _offsetBit % 8
                          : _deviceSize == XGTDeviceSize.Byte ? 1
                          : _deviceSize == XGTDeviceSize.Word ? 2
                          : _deviceSize == XGTDeviceSize.DWord ? 4
                          : _deviceSize == XGTDeviceSize.LWord ? 8
                          : throw new Exception($"Size value error : current {_deviceSize}");


        public int Offset => _offsetBit / 8;
        public abstract string ToTextValue();
    }

    /// <summary>
    /// Represents a specific type of XGT device, specifically a bit.
    /// </summary>
    public class XGTDeviceBit : XGTDevice
    {
        private bool value;
        public bool Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    ChangeValueSubject.OnNext(this);
                }
            }
        }

        public XGTDeviceBit(char device, int offsetBit)
            : base(device, XGTDeviceSize.Bit, offsetBit)
        {
        }

        public override string ToTextValue()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// Represents a byte type XGT device.
    /// </summary>
    public class XGTDeviceByte : XGTDevice
    {
        private byte value;
        public byte Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    ChangeValueSubject.OnNext(this);
                }
            }
        }


        public XGTDeviceByte(char device, int offsetBit)
            : base(device, XGTDeviceSize.Byte, offsetBit)
        {
        }

        public override string ToTextValue()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// Represents a word type XGT device.
    /// </summary>
    public class XGTDeviceWord : XGTDevice
    {
        private ushort value;
        public ushort Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    ChangeValueSubject.OnNext(this);
                }
            }
        }


        public XGTDeviceWord(char device, int offsetBit)
            : base(device, XGTDeviceSize.Word, offsetBit)
        {
        }

        public override string ToTextValue()
        {
            return Value.ToString();
        }
    }
    /// <summary>
    /// Represents a double word (DWord) type XGT device.
    /// </summary>
    public class XGTDeviceDWord : XGTDevice
    {
        private uint value;
        public uint Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    ChangeValueSubject.OnNext(this);
                }
            }
        }

        public XGTDeviceDWord(char device, int offsetBit)
            : base(device, XGTDeviceSize.DWord, offsetBit)
        {
        }

        public override string ToTextValue()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// Represents a long word (LWord) type XGT device.
    /// </summary>
    public class XGTDeviceLWord : XGTDevice
    {
        private ulong value;
        public ulong Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    ChangeValueSubject.OnNext(this);
                }
            }
        }

        public XGTDeviceLWord(char device, int offsetBit)
            : base(device, XGTDeviceSize.LWord, offsetBit)
        {
        }

        public override string ToTextValue()
        {
            return Value.ToString();
        }
    }
}
