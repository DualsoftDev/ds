using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Engine.Core.GlobalShortCuts;
using static FParsec.ErrorMessage;


namespace Engine
{
    internal class TagChangeChecker
    {
        public TagChangeChecker(OpcBroker opc, IEnumerable<(string, Func<Bit, bool, Task>)> actions)
        {
            var tagName2ActionMap = new Dictionary<string, Func<Bit, bool, Task>>();

            foreach (var (bit, action) in actions)
                tagName2ActionMap.Add(bit, action);

            Global.BitChangedSubject
                .Where(bc => bc.Bit is Bit)
                .Subscribe(async bc =>
                {
                    var bit = bc.Bit as Bit;
                    var n = bit.GetName();
                    if (tagName2ActionMap.ContainsKey(n) && bit is TagA)
                        await Task.Run(async () =>
                        {
                            await tagName2ActionMap[n].Invoke(bit, bc.NewValue);
                        });
                });
        }

    }

    internal class InterlockChecker : TagChangeChecker
    {
        public InterlockChecker(OpcBroker opc, IEnumerable<(string, Func<Bit, bool, Task>)> actions)
            : base(opc, actions)
        {
        }

        public static InterlockChecker CreateFromCylinder(OpcBroker opc, IEnumerable<string> cylinderFlowNames)   // e.g {"A_F", "B_F"}
        {
            Func<Bit, bool, Task> interlockChecker(string interlockName) =>
                (bit, val) =>
                    Task.Run(() => {    // caller 에서 await 함..
                        var opcOpposite = opc.GetTag(interlockName);
                        Global.Verify($"Exclusive error: {bit.Name}", !val || opcOpposite.Value == false);
                    }); // no .FireAndForget();

            IEnumerable<(string, Func<Bit, bool, Task>)> generateMap()
            {
                foreach (var f in cylinderFlowNames)
                {
                    yield return ($"StartActual_{f}_Vp", interlockChecker($"StartActual_{f}_Vm"));
                    yield return ($"StartActual_{f}_Vm", interlockChecker($"StartActual_{f}_Vp"));
                    yield return ($"EndActual_{  f}_Sp", interlockChecker($"EndActual_{  f}_Sm"));
                    yield return ($"EndActual_{  f}_Sm", interlockChecker($"EndActual_{  f}_Sp"));
                }
            }
            var cmd2sensors = generateMap().ToArray();
            return new InterlockChecker(opc, cmd2sensors);
        }
    }


    internal class Simulator : TagChangeChecker
    {
        public Simulator(OpcBroker opc, IEnumerable<(string, Func<Bit, bool, Task>)> actions)
            : base(opc, actions)
        {
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
                    {
                        var (me, other) = ($"StartActual_{f}_Vp", $"StartActual_{f}_Vm");
                        yield return (me,
                            (bit, val) => Task.Run(async () =>
                            {
                                bool keepGo = true;
                                using var _ =
                                    Global.DebugNotifyingSubject
                                    .Subscribe(tpl =>
                                    {
                                        var (tag, value) = tpl;
                                        if (tag == me && !value && Global.IsDebugStopAndGoStressMode)
                                        {
                                            LogDebug($"출력 단절 {tpl.Item1} detected");
                                            keepGo = false;
                                        }
                                    });
                                var opcOpposite = opc.GetTag(other);
                                LogDebug($"Simulating Tag/Actuator {bit.Name} = {val}");
                                if (val)
                                {
                                    Console.Beep(800, 200);
                                    Global.Verify($"Exclusive error: {bit.Name}", opcOpposite.Value == false);
                                    await Task.Delay(random.Next(5, 50));
                                    if (!keepGo)
                                        return;
                                    opc.Write($"EndActual_{f}_Sm", !val);
                                    await Task.Delay(random.Next(5, 1000));
                                    if (!keepGo)
                                        return;
                                    Global.Verify($"유지:{bit.Name}", bit.Value);
                                    opc.Write($"EndActual_{f}_Sp", val);
                                }
                            }));
                    }


                    {
                        var (me, other) = ($"StartActual_{f}_Vm", $"StartActual_{f}_Vp");
                        yield return (me,
                            (bit, val) => Task.Run(async () =>
                            {
                                bool keepGo = true;
                                using var _ =
                                    Global.DebugNotifyingSubject
                                    .Where(tpl => tpl.Item1 == other && !tpl.Item2)
                                    .Subscribe(tpl =>
                                    {
                                        if (Global.IsDebugStopAndGoStressMode)
                                        {
                                            LogDebug($"출력 단절 {tpl.Item1} detected");
                                            keepGo = false;
                                        }
                                    });
                                var opcOpposite = opc.GetTag(other);
                                LogDebug($"Tag/Actuator {bit.Name} = {val}");
                                if (val)
                                {
                                    Console.Beep(1600, 200);
                                    Global.Verify($"Exclusive error: {bit.Name}", opcOpposite.Value == false);
                                    await Task.Delay(random.Next(5, 50));
                                    if (!keepGo)
                                        return;
                                    opc.Write($"EndActual_{f}_Sp", !val);
                                    await Task.Delay(random.Next(5, 1000));
                                    if (!keepGo)
                                        return;
                                    Global.Verify($"유지:{bit.Name}", bit.Value);
                                    opc.Write($"EndActual_{f}_Sm", val);
                                }
                            }));
                    }
                }
            }
            var cmd2sensors = generateMap().ToArray();
            return new Simulator(opc, cmd2sensors);
        }
    }
}
