using Engine.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using static Microsoft.FSharp.Core.ByRefKinds;

namespace Engine
{
    internal class Simulator
    {
        // e.g cmd2sensors = {("StartActual_A_F_Vp", "EndActual_A_F_Sm"), }
        public Simulator(OpcBroker opc, IEnumerable<(string, Action<Bit, bool>)> actions)
        {
            var c2sDic = new Dictionary<string, Action<Bit, bool>>();

            foreach (var (bit, action) in actions)
                c2sDic.Add(bit, action);

            Global.BitChangedSubject
                .Where(bc => bc.Bit is Bit)
                .Subscribe(async bc =>
                {
                    var bit = bc.Bit as Bit;
                    var n = bit.GetName();
                    if (c2sDic.ContainsKey(n))
                        c2sDic[n].Invoke(bit, bc.NewValue);
                });
        }

        public static Simulator CreateFromCylinder(OpcBroker opc, IEnumerable<string> cylinderFlowNames)   // e.g {"A_F", "B_F"}
        {
            var random = new Random();
            IEnumerable<(string, Action<Bit, bool>)> generateMap()
            {
                foreach (var f in cylinderFlowNames)
                {
                    yield return ($"StartActual_{f}_Vp",
                        new Action<Bit, bool>(async (bit, val) =>
                        {
                            var opcOpposite = opc.GetTag($"StartActual_{f}_Vm");
                            if (val)
                            {
                                Debug.Assert(opcOpposite.Value == false);
                                await Task.Delay(random.Next(5, 50));
                                opc.Write($"EndActual_{f}_Sm", !val);
                                await Task.Delay(random.Next(10, 500));
                                opc.Write($"EndActual_{f}_Sp", val);
                            }
                        }));
                    yield return ($"StartActual_{f}_Vm",
                        new Action<Bit, bool>(async (bit, val) =>
                        {
                            var opcOpposite = opc.GetTag($"StartActual_{f}_Vp");
                            if (val)
                            {
                                Debug.Assert(opcOpposite.Value == false);
                                await Task.Delay(random.Next(5, 50));
                                opc.Write($"EndActual_{f}_Sp", !val);
                                await Task.Delay(random.Next(10, 500));
                                opc.Write($"EndActual_{f}_Sm", val);
                            }
                        }));
                }
            }
            var cmd2sensors = generateMap().ToArray();
            return new Simulator(opc, cmd2sensors);
        }
    }
}
