using IO.Spec;

namespace ThirdParty.AddressInfo.Provider
{
    public class AddressInfoProviderPaix : IAddressInfoProvider
    {
        public bool GetAddressInfo(string address, out string memoryType, out int byteOffset, out int bitOffset, out int contentBitLength)
        {
            bitOffset = byteOffset = contentBitLength = 0;
            memoryType = string.Empty;

            try
            {
                address = address.ToLower().TrimStart('%');
                if (address[0] == 'i' || address[0] == 'o')
                {
                    memoryType = address[0].ToString();
                    int read1(string addr) => int.Parse(addr);
                    (int, int) read2(string addr)
                    {
                        int byteOffset = 0;
                        int bitOffset = 0;
                        if (addr.Contains('.'))
                        {
                            var addrSplit = addr.Split('.');
                            byteOffset = int.Parse(addrSplit[0]);
                            bitOffset = int.Parse(addrSplit[1]);
                        }
                        else
                        {
                            var n = int.Parse(addr);
                            byteOffset = n / 8;
                            bitOffset = n % 8;
                        }
                        return (byteOffset, bitOffset);
                    }

                    string addr = address[2..];
                    switch (address[1])
                    {
                        case 'x':
                            contentBitLength = 1;
                            (byteOffset, bitOffset) = read2(addr);
                            break;
                        case 'b':
                            contentBitLength = 8;
                            byteOffset = read1(addr);
                            break;
                        case 'w':
                            contentBitLength = 16;
                            byteOffset = read1(addr) * 2;
                            break;
                        case 'd':
                            contentBitLength = 32;
                            byteOffset = read1(addr) * 4;
                            break;
                        case 'l':
                            contentBitLength = 64;
                            byteOffset = read1(addr) * 8;
                            break;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
            }

            return false;
        }


        public string GetTagName(string memoryType, int byteOffset, int bitOffset, int contentBitLength)
        {
            return contentBitLength switch
            {
                1 => $"{memoryType}x{byteOffset * 8 + bitOffset}",
                8 => $"{memoryType}b{byteOffset}",
                16 => $"{memoryType}w{byteOffset / 2}",
                32 => $"{memoryType}dw{byteOffset / 4}",
                64 => $"{memoryType}lw{byteOffset / 8}",
                _ => throw new Exception($"Unknown content bit size: {contentBitLength}"),
            };
        }
    }
}

