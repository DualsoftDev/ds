using System;
namespace Server.Common;

public interface IBridgeHandler
{
    void Transfer(short _idx, short _onoff);
    void Receive(Action<short[], string> _receiver);
}