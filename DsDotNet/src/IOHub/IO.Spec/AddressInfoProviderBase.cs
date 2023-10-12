namespace IO.Spec
{
    public interface IAddressInfoProvider
    {
        bool GetAddressInfo(string address, out int byteOffset, out int bitOffset, out int contentBitLength);
    }
    //public abstract class AddressInfoProviderBase
    //{

    //}
}