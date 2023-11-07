using IO.Spec;

namespace ThirdParty.AddressInfo.Provider
{
    public class AddressInfoProviderLsXGI : IAddressInfoProvider
    {
        public bool GetAddressInfo(string address, out string memoryType, out int offset, out int contentBitLength)
        {
            offset = contentBitLength = 0;
            memoryType = string.Empty;

            try
            {
                address = address.ToLower().TrimStart('%');
                if (address[0] == 'i' || address[0] == 'q' || address[0] == 'm')
                {
                    memoryType = address[0].ToString();
                    string addr = address[2..];
                    offset = int.Parse(addr);
                    contentBitLength = address[1] switch
                    {
                        'x' => 1,
                        'b' => 8,
                        'w' => 16,
                        'd' => 32,
                        'l' => 64,
                        _ => throw new Exception($"Unknown content bit size: {address[1]}"),
                    };

                    return true;
                }
            }
            catch (Exception ex)
            {
            }

            return false;
        }

        public string GetTagName(string memoryType, int offset, int contentBitLength)
        {
            var dataType = contentBitLength switch
            {
                1 => "x",
                8 => "b",
                16 => "w",
                32 => "dw",
                64 => "lw",
                _ => throw new Exception($"Unknown content bit size: {contentBitLength}"),
            };

            return $"{memoryType}{dataType}{offset}";
        }

    }
}