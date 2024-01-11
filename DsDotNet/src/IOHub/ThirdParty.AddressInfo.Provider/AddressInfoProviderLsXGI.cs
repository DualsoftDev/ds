using IO.Spec;
using System.ComponentModel.DataAnnotations;
using XGTComm;

namespace ThirdParty.AddressInfo.Provider;

public class AddressInfoProviderLsXGI : IAddressInfoProvider
{
    public bool GetAddressInfo(string address, out string memoryType, out int offset, out int contentBitLength)
    {
        var tag = XGTComm.XGTParserXGI.LsTagXGIPattern(address);
        if (tag == null)
        {
            offset = 0;
            memoryType = string.Empty;
            contentBitLength = 0;

            return false;
        }
        else
        {
            memoryType = tag.Item1;
            contentBitLength = XGTParserUtil.GetBitLength(tag.Item2);
            offset = tag.Item3;
            return false;
        }
    }

    public string GetTagName(string memoryType, int offset, int dataSize)
    {
        var dataType = dataSize switch
        {
            1 => "x",
            8 => "b",
            16 => "w",
            32 => "d",
            64 => "l",
            _ => throw new Exception($"Unknown content bit size: {dataSize}"),
        };


        var dev = memoryType.ToLower() switch
        {
            "i" => getIODev("i", offset, dataSize, dataType),
            "q" => getIODev("q", offset, dataSize, dataType),
            "m" => getMemDev("m", offset, dataSize, dataType),
            "r" => getMemDev("r", offset, dataSize, dataType),

            _ => throw new Exception($"Unknown memoryType: {memoryType}"),
        };

        return dev.ToUpper();

        static string getIODev(string mem, int offset, int sizeType, string dataType)
        {
            var ioSlotCardType = 64; //test ahn 카드정보 받아서 수정 필요
            var ioSlotCont = 16;
            return $"%{mem}{dataType}{offset / ioSlotCardType / ioSlotCont}.{offset / ioSlotCardType % ioSlotCont}.{offset % ioSlotCardType}";
        }
        static string getMemDev(string mem, int offset, int sizeType, string dataType)
        {
            return sizeType == 1 ? $"%{mem}{dataType}{offset}" : $"%{mem}{dataType}{offset / sizeType}.{offset % sizeType}";
        }
      
    }

}