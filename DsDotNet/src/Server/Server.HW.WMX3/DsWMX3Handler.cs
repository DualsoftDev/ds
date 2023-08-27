using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using WMX3ApiCLR;

namespace Server.HW.WMX3;

public static class HWEvent
{
    public static Subject<Tuple<int, bool>> ValueChangeSubjectPaixInputs = new Subject<Tuple<int, bool>>();
}

public class DsPaixHandlerWMX3 
{
    private WMX3Api Wmx3Lib;
    private EngineStatus EnStatus;
    private Io Wmx3Lib_Io;
    private byte[] _inData;
    private byte[] _outData;

    public bool IsAvailable { get; private set; }
    public bool Running { get; private set; }
    /// <summary>
    /// TimeOutConnect msec
    /// </summary>
    public int TimeOutConnect { get; private set; } = 1000;
    /// <summary>
    /// TimeOutScanIO msec
    /// </summary>
    public int TimeOutScanIO { get; private set; } = 10;
    public DsPaixHandlerWMX3(int numIn, int numOut)
    {
        Wmx3Lib    = new WMX3Api();
        EnStatus   = new EngineStatus();
        Wmx3Lib_Io = new Io(Wmx3Lib);
        _inData     = Enumerable.Repeat((byte)0, count: numIn).ToArray();
        _outData    = Enumerable.Repeat((byte)0, count: numOut).ToArray();
        Wmx3Lib.CreateDevice("C:\\Program Files\\SoftServo\\WMX3\\",//설치 PC만 사용 가능  ip설정 필요없음
                    DeviceType.DeviceTypeNormal, (uint)TimeOutConnect);

        SpinWait.SpinUntil(() =>
        {
            Wmx3Lib.StartCommunication((uint)TimeOutConnect);
            Wmx3Lib.GetEngineStatus(ref EnStatus);
            if (EnStatus.State == EngineState.Communicating)
                IsAvailable = true;

            return IsAvailable;
        }, new TimeSpan(0, 0, 0, 0, TimeOutConnect) );


    }

    public void Dispose()
    {
        // Stop Communication.
        Wmx3Lib.StopCommunication((uint)TimeOutConnect);
        // Discard the device.
        Wmx3Lib.CloseDevice();
        Wmx3Lib_Io.Dispose();
        Wmx3Lib.Dispose();
    }


   
    public void SetOutBit(int idx, bool onoff)
    {
        if (IsAvailable)
        {
            var byteIdx = idx / 8;
            var bitIdx = idx % 8;
            var value = Convert.ToByte(onoff);
            Wmx3Lib_Io.SetOutBit(byteIdx, bitIdx, value);
        }
    }

    public void Run()
    {
        Running = true;
        Task.Run(async () =>
        {
            while (Running)
            {
                await Task.Delay(TimeOutScanIO);
                Wmx3Lib_Io.GetInBytes(0, _inData.Length, ref _inData);

                var oldData = _inData.ToList();
                for (int iByte = 0; iByte < _inData.Length; iByte++)
                {
                    if (_inData[iByte] == oldData[iByte])
                        continue;
                    var oldBits = new BitArray(oldData[iByte]);
                    var newBits = new BitArray(_inData[iByte]);

                    for (int iBit = 0; iBit < newBits.Length; iBit++)
                        if (oldBits[iBit] != newBits[iBit])
                            HWEvent.ValueChangeSubjectPaixInputs.OnNext(Tuple.Create(iBit, newBits[iBit]));
                }
            }
        });
    }

    public void Stop()
    {
        Running = false;
    }


}