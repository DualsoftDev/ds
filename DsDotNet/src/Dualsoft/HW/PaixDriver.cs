using Server.Common.NMC;
using Server.Common.NMF;
using Server.HW.WMX3;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DSModeler
{
    public class PaixDriver
    {
        public string IP { get; set; }
        public bool Running { get; private set; }
        public short[] NumIn { get; set; }
        public short[] NumOut { get; set; }
        public PaixDriver(string ip, int numIn, int numOut)
        {
            IP = ip;
            NumIn = new short[numIn];
            NumOut = new short[numOut];
        }

        public bool Open()
        {
            IPAddress.TryParse(IP, out IPAddress addr);
            if (addr == null)
            { MBox.Error($"{IP} ip 형식으로 올바르지 않습니다."); return false; }
            if (PaixDrivers.Ping(IP) == 0)
                return PaixDrivers.Open(IP) == 0;
            else
                return false;
        }

        public bool Dispose()
        {
            Running = false;
            PaixDrivers.Close(IP);
            return true;
        }


        public void Run()
        {
            Running = true;
            Task.Run(async () =>
            {
                while (Running)
                {
                    await Task.Delay(10);
                    var oldNum = NumIn.ToList();
                    PaixDrivers.GetInput(IP, NumIn);
                    for (int iByte = 0; iByte < NumIn.Length; iByte++)
                    {
                        if (NumIn[iByte] == oldNum[iByte])
                            continue;
                        var oldShort = new BitArray(oldNum[iByte]);
                        var newShort = new BitArray(NumIn[iByte]);

                        for (int iBit = 0; iBit < oldShort.Length; iBit++) //short size
                            if (oldShort[iBit] != newShort[iBit])
                                HWEvent.ValueChangeSubjectPaixInputs.OnNext(Tuple.Create(iBit, newShort[iBit]));
                    }
                }
            });
        }

        BitArray getBitArray(short[] shortArr)
        {
            var arrByte = Array.ConvertAll<short, byte>(shortArr, delegate (short item) { return (byte)item; });
            return new BitArray(arrByte);
        }

        public void Stop()
        {
            Running = false;
        }

        public void SetBit(short index, bool bOn)
        {
            if (Running)
                PaixDrivers.SetOutput(IP, index, Convert.ToInt16(bOn));
            else
                MBox.Error($"{IP} Run을 수행을 먼저하세요");
        }
    }
}


