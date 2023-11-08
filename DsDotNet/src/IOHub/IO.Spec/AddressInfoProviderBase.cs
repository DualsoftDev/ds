namespace IO.Spec
{
    public interface IAddressInfoProvider
    {
        bool GetAddressInfo(string address, out string memoryType, out int offset, out int contentBitLength);
        string GetTagName(string memoryType, int offset, int contentBitLength);
    }
}