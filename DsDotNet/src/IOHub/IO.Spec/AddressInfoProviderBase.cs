namespace IO.Spec
{
    public interface IAddressInfoProvider
    {
        bool GetAddressInfo(string address, out string memoryType, out int byteOffset, out int bitOffset, out int contentBitLength);
        string GetTagName(string memoryType, int byteOffset, int bitOffset, int contentBitLength);
    }
    public abstract class AddressInfoProviderBase : IAddressInfoProvider
    {
        public abstract bool GetAddressInfo(string address, out string memoryType, out int byteOffset, out int bitOffset, out int contentBitLength);
        public abstract string GetTagName(string memoryType, int byteOffset, int bitOffset, int contentBitLength);
    }

    public class AddressInfoProviderLsXGI : IAddressInfoProvider
    {
        public bool GetAddressInfo(string address, out string memoryType, out int byteOffset, out int bitOffset, out int contentBitLength)
        {
            throw new NotImplementedException();
        }
        public string GetTagName(string memoryType, int byteOffset, int bitOffset, int contentBitLength) { throw new NotImplementedException(); }
    }

    public class AddressInfoProviderPaix : IAddressInfoProvider
    {
        public bool GetAddressInfo(string address, out string memoryType, out int byteOffset, out int bitOffset, out int contentBitLength)
        {
            throw new NotImplementedException();
        }
        public string GetTagName(string memoryType, int byteOffset, int bitOffset, int contentBitLength) { throw new NotImplementedException(); }
    }

}