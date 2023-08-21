using Server.Common.NMC;
using Server.Common.NMF;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DSModeler
{
    

    public static class PaixDrivers
    {
        public static PaixHW SelectPaixHW { get; set; } = PaixHW.NMC2;
        public static short[] GetIPSplit(string ipText) => ipText.Split('.').Select(s => Convert.ToInt16(s)).ToArray();
        public enum PaixHW
        {
            NMC2,
            NMF
        }

        public static short Ping(string ipText)
        {
            var ip = GetIPSplit(ipText);
            short ret = 0;
            switch (SelectPaixHW)
            {
                case PaixHW.NMC2: ret = NMC2.nmc_PingCheck(ip[3], 500); break;
                case PaixHW.NMF: ret = NMF.nmf_PingCheck(ip[3], ip[0], ip[1], ip[2], 500); break;
                default:
                    new Exception($"HW {SelectPaixHW} is not supported");
                    break;
            }

            return ret;
        }
        public static short Open(string ipText)
        {
            var ip = GetIPSplit(ipText);
            short ret = 0;

            switch (SelectPaixHW)
            {
                case PaixHW.NMC2: ret = NMC2.nmc_OpenDevice(ip[3]); break;
                case PaixHW.NMF: ret = NMF.nmf_Connect(ip[3], ip[0], ip[1], ip[2]); break;
                default:
                    new Exception($"HW {SelectPaixHW} is not supported");
                    break;
            }

            return ret;
        }

        public static bool Close(string ipText)
        {
            var ip = GetIPSplit(ipText);

            switch (SelectPaixHW)
            {
                case PaixHW.NMC2: NMC2.nmc_CloseDevice(ip[3]); break;
                case PaixHW.NMF: NMF.nmf_Disconnect(ip[3]); break;
                default:
                    new Exception($"HW {SelectPaixHW} is not supported");
                    break;
            }

            return true;
        }
        public static short GetInput(string ipText, short[] inputs)
        {
            var ip = GetIPSplit(ipText);
            short ret = 0;
            switch (SelectPaixHW)
            {
                case PaixHW.NMC2: ret = NMC2.nmc_GetDIOInput128(ip[3], inputs); break;
                case PaixHW.NMF: ret = NMF.nmf_DIGet(ip[3], inputs); break;
                default:
                    new Exception($"HW {SelectPaixHW} is not supported");
                    break;
            }

            return ret;
        }

        public static void SetOutput(string ipText, short index, short value)
        {
            var ip = GetIPSplit(ipText);

            switch (SelectPaixHW)
            {
                case PaixHW.NMC2: NMC2.nmc_SetDIOOutPin(ip[3], index, value); break;
                case PaixHW.NMF: NMF.nmf_DOSetPin(ip[3], index, value); break;
                default:
                    new Exception($"HW {SelectPaixHW} is not supported");
                    break;
            }
        }
    }
}


