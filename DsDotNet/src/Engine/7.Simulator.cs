using Engine.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Microsoft.FSharp.Core.ByRefKinds;

namespace Engine
{
    internal class Simulator
    {
        // e.g cmd2sensors = {("StartActual_A_F_Vp", "EndActual_A_F_Sm"), }
        public Simulator(OpcBroker opc, IEnumerable<(string, string)> cmd2sensors, IEnumerable<(string, string)> mutualExclusives)
        {
            var random = new Random();
            var c2sDic = new Dictionary<string, string>();
            var mutexDic = new Dictionary<string, string>();

            foreach (var (command, sensor) in cmd2sensors)
                c2sDic.Add(command, sensor);
            foreach (var (plus, minus) in mutualExclusives)
            {
                mutexDic.Add(plus, minus);
                mutexDic.Add(minus, plus);
            }

            Global.BitChangedSubject
                .Subscribe(async bc =>
                {
                    var n = bc.Bit.GetName();
                    var val = bc.Bit.Value;

                    if (val && mutexDic.ContainsKey(n))
                    {
                        var opcTag = opc.GetTag(mutexDic[n]);
                        Debug.Assert(!opcTag.Value);
                    }

                    if (c2sDic.ContainsKey(n))
                    {
                        await Task.Delay(random.Next(10, 500));
                        opc.Write(c2sDic[n], val);
                    }
                });
        }

        public static Simulator CreateFromCylinder(OpcBroker opc, IEnumerable<string> cylinderFlowNames)   // e.g {"A_F", "B_F"}
        {
            IEnumerable<(string, string)> generateMap()
            {
                foreach (var f in cylinderFlowNames)
                {
                    yield return ($"StartActual_{f}_Vp", $"EndActual_{f}_Sp");
                    yield return ($"StartActual_{f}_Vm", $"EndActual_{f}_Sm");
                }
            }
            IEnumerable<(string, string)> generateExclusivesMap()
            {
                foreach (var f in cylinderFlowNames)
                {
                    yield return ($"StartActual_{f}_Vp", $"StartActual_{f}_Vm");
                    yield return ($"EndActual_{f}_Sp", $"EndActual_{f}_Sm");
                }
            }

            var cmd2sensors = generateMap().ToArray();
            var exclusives = generateExclusivesMap().ToArray();
            return new Simulator(opc, cmd2sensors, exclusives);
        }
    }
}
