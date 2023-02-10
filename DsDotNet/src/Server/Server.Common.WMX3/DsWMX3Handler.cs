using System;
using System.Collections;
using System.Collections.Generic;
using Server.Common;
using WMX3ApiCLR;

namespace Server.Common.WMX3;

public class DsPaixHandlerNMC : IBridgeHandler
{
    private WMX3Api Wmx3Lib;
    private EngineStatus EnStatus;
    private Io Wmx3Lib_Io;
    private bool isAvailable;
    private byte[] inData;
    private byte[] outData;

    public DsPaixHandlerNMC(int _numIn, int _numOut)
    {
        Wmx3Lib    = new WMX3Api();
        EnStatus   = new EngineStatus();
        Wmx3Lib_Io = new Io(Wmx3Lib);
        inData     = Enumerable.Repeat((byte)0, count: _numIn).ToArray();
        outData    = Enumerable.Repeat((byte)0, count: _numOut).ToArray();

        while (true)
        {
            Wmx3Lib.StartCommunication(0xFFFFFFFF);
            Wmx3Lib.GetEngineStatus(ref EnStatus);
            if (EnStatus.State == EngineState.Communicating)
            {
                isAvailable = true;
                break;
            }
        }
    }

    ~DsPaixHandlerNMC()
    {
        // Stop Communication.
        Wmx3Lib.StopCommunication(0xFFFFFFFF);
        // Discard the device.
        Wmx3Lib.CloseDevice();
        Wmx3Lib_Io.Dispose();
        Wmx3Lib.Dispose();
    }

    public void Transfer(short _idx, short _onoff)
    {
        if (isAvailable)
            Console.WriteLine("available");
    }

    public void Receive(Action<short[], string> _receiver)
    {

    }
}