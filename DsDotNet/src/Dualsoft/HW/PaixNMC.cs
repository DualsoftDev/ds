using Server.Common.NMC;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DSModeler
{
    public class PaixNMC
    {
        public string IP { get; set; }
        public bool Running { get; private set; }
        public short IPNum => Convert.ToSByte(IP.Split('.')[3]);
        public short[] NumIn { get; set; }
        public short[] NumOut { get; set; }
        public PaixNMC(string ip, int numIn, int numOut)
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

            var nRet = NMC2.nmc_PingCheck(IPNum, 500);
            if (nRet == 0)
                return NMC2.nmc_OpenDevice(IPNum) == 0;
            else
                return false;
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
                    NMC2.nmc_GetDIOInput(IPNum, NumIn);

                    if (!oldNum.ToArray().Equals(NumIn))
                    {
                        var oldBits = getBitArray(oldNum.ToArray());
                        var newBits = getBitArray(NumIn);
                        var diffBits = oldBits.Xor(newBits);
                        for (int i = 0; i < diffBits.Count; i++)
                            Global.ValueChangeSubjectPaix.OnNext(Tuple.Create(i, diffBits[i]));
                    }

                    BitArray getBitArray(short[] shortArr)
                    {
                        var arrByte = Array.ConvertAll<short, byte>(shortArr, delegate (short item) { return (byte)item; });
                        return new BitArray(arrByte);
                    }
                }
            });
        }


        public void Stop()
        {
            Running = false;
        }

        public void SetBit(short index, bool bOn)
        {
            if (Running)
                NMC2.nmc_SetDIOOutPin(IPNum, index, Convert.ToInt16(bOn));
            else
                MBox.Error($"{IP} Run을 수행을 먼저하세요");
        }
        public bool? GetBit(short index)
        {
            if (Running)
            {
                NMC2.nmc_GetDIOInPin(IPNum, index, out short data);
                return Convert.ToBoolean(data);
            }
            else
            {
                MBox.Error($"{IP} Run을 수행을 먼저하세요");
                return null;
            }
        }
    }
}


