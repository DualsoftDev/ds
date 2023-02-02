using System;
using System.Linq;
using Server.Common;

namespace Server.Common.NMC;

public class DsPaixHandler:IBridgeHandler
{
    private readonly short ip;
    private bool isAvailable;
    private short[] input;

    public DsPaixHandler(short _ip, int _numIO)
    {
        ip = _ip;
        input = Enumerable.Repeat((short)0, count: _numIO).ToArray();
        isAvailable = false;
    }

    private bool PingChecker()
    {
        var nRet = NMC2.nmc_PingCheck(ip, 50);
        if (nRet != 0)
        {
            Console.WriteLine("nmc_PingCheck error");
            return false;
        }
        return true;
    }

    public void Transfer(short _idx, short _onoff)
    {
        if (isAvailable)
            NMC2.nmc_SetDIOOutputBit(ip, _idx, _onoff);
    }

    public void Receive(Action<short[], string> _receiver)
    {
        if (!PingChecker())
            return;

        if (NMC2.nmc_OpenDevice(ip) != 0)
            return;

        isAvailable = true;
        while (true)
        {
            NMC2.nmc_GetDIOInput(ip, input);
            _receiver(input, "paix");
        }
    }
}