using System.Reactive.Linq;
using System.Threading.Tasks;

using static Engine.Core.GlobalShortCuts;


namespace Engine
{
    internal class Simulator
    {
        // e.g cmd2sensors = {("StartActual_A_F_Vp", "EndActual_A_F_Sm"), }
        public Simulator(OpcBroker opc, IEnumerable<(string, Func<Bit, bool, Task>)> actions)
        {
            var c2sDic = new Dictionary<string, Func<Bit, bool, Task>>();

            foreach (var (bit, action) in actions)
                c2sDic.Add(bit, action);

            Global.BitChangedSubject
                .Where(bc => bc.Bit is Bit)
                .Subscribe(async bc =>
                {
                    var bit = bc.Bit as Bit;
                    var n = bit.GetName();
                    if (c2sDic.ContainsKey(n) && bit is TagA)
                        await Task.Run(async () =>
                        {
                            await c2sDic[n].Invoke(bit, bc.NewValue);
                        });
                });
        }

        public static Simulator CreateFromCylinder(OpcBroker opc, IEnumerable<string> cylinderFlowNames)   // e.g {"A_F", "B_F"}
        {
            if (Global.IsControlMode)
                throw new Exception("Simulation not supported in real control mode.");

            var random = new Random();
            IEnumerable<(string, Func<Bit, bool, Task>)> generateMap()
            {
                foreach (var f in cylinderFlowNames)
                {
                    yield return ($"StartActual_{f}_Vp",
                        (bit, val) => Task.Run(async () => {
                            var opcOpposite = opc.GetTag($"StartActual_{f}_Vm");
                            LogDebug($"Tag/Actuator {bit.Name} = {val}");
                            if (val)
                            {
                                Console.Beep(800, 200);
                                Global.Verify($"Exclusive error: {bit.Name}", opcOpposite.Value == false);
                                await Task.Delay(random.Next(5, 50));
                                opc.Write($"EndActual_{f}_Sm", !val);
                                await Task.Delay(random.Next(5, 1000));
                                Global.Verify($"유지:{bit.Name}", bit.Value);
                                opc.Write($"EndActual_{f}_Sp", val);
                            }
                        }));
                            
                    yield return ($"StartActual_{f}_Vm",
                        (bit, val) => Task.Run(async () => {
                            var opcOpposite = opc.GetTag($"StartActual_{f}_Vp");
                            LogDebug($"Tag/Actuator {bit.Name} = {val}");
                            if (val)
                            {
                                Console.Beep(1600, 200);
                                Global.Verify($"Exclusive error: {bit.Name}", opcOpposite.Value == false);
                                await Task.Delay(random.Next(5, 50));
                                opc.Write($"EndActual_{f}_Sp", !val);
                                await Task.Delay(random.Next(5, 1000));
                                Global.Verify($"유지:{bit.Name}", bit.Value);
                                opc.Write($"EndActual_{f}_Sm", val);
                            }
                        }));

                    yield return ($"EndActual_{f}_Sp",
                        (bit, val) => Task.Run(async () => {
                            LogDebug($"Tag/Sensor {bit.Name} = {val}");
                            var opcOpposite = opc.GetTag($"EndActual_{f}_Sm");
                            Global.Verify($"Exclusive error: {bit.Name}", !val || opcOpposite.Value == false);
                        }));

                    yield return ($"EndActual_{f}_Sm",
                        (bit, val) => Task.Run(async () => {
                            LogDebug($"Tag/Sensor {bit.Name} = {val}");
                            var opcOpposite = opc.GetTag($"EndActual_{f}_Sp");
                            Global.Verify($"Exclusive error: {bit.Name}", !val || opcOpposite.Value == false);
                        }));
                }
            }
            var cmd2sensors = generateMap().ToArray();
            return new Simulator(opc, cmd2sensors);
        }
    }
}
