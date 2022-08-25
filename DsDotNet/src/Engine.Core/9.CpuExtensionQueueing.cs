using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Core;

/// <summary>
/// Queue 이용한 구현
/// </summary>
public static class CpuExtensionQueueing
{
    public static IDisposable Run(this Cpu cpu)
    {
        var disposable = new CancellationDisposable();
        var q = cpu.Queue;

        cpu.BuildFlipFlopMapOnDemand();

#if DEBUG
        new Thread(() =>
#else
        new Thread(async () =>
#endif
        {
            cpu.ThreadId = Thread.CurrentThread.ManagedThreadId;
            Global.Logger.Debug($"\tRunning {cpu.ToText()}");

            while (!disposable.IsDisposed && cpu.Running)
            {
                while (q.Count > 0 && cpu.Running)
                {
                    cpu.ProcessingQueue = true;
                    if (q.TryDequeue(out BitChange bitChange))
                    {
                        if (bitChange.Bit.GetName() == "AutoStart_L_F")
                            Global.NoOp();
                        cpu.Apply(bitChange, true);
                    }
                    else
                        Global.Logger.Warn($"Failed to deque.");
                }
                cpu.ProcessingQueue = false;

#if DEBUG
                Thread.Sleep(5);
#else
                await Task.Delay(5);
#endif
            }
        }).Start()
        ;


        return disposable;
    }
    public static void Apply(this Cpu cpu, BitChange bitChange, bool withQueue)
    {
        //if (bitChange.Bit.Value == bitChange.NewValue)
        //    return;

        Global.Logger.Debug($"\t\t=[{cpu.NestingLevel}] Applying bitChange {bitChange}");   // {bitChange.Guid}

        var fwd = cpu.ForwardDependancyMap;
        var q = cpu.Queue;

        Debug.Assert(cpu.FFSetterMap != null);
        cpu.BuildFlipFlopMapOnDemand();

        cpu.NestingLevel++;
        var bit = (Bit)bitChange.Bit;

        // { debug
        if (bitChange is PortInfoPlanChange pipc)
        {
            var contain = fwd.ContainsKey(bit);
            Global.NoOp();
        }
        if (bitChange.Bit.GetName() == "EndActual_A_F_Sm")  //"ResetPlan_A_F_Sm")  //"StartPlan_A_F_Vm") //"InnerStartSourceFF_VPS_A_F_Pp_Vp")   // "StartPlanAnd_VPS_A_F_Pp")
            Global.NoOp();

        IEnumerable<IBit> collectForwards(IBit bit)
        {
            if (fwd.ContainsKey(bit))
            {
                var dependents = fwd[bit];
                foreach (var d in dependents)
                {
                    yield return d;
                    if (d is not IBitWritable)
                        foreach (var dd in collectForwards(d))
                            yield return dd;
                }
            }
        }

        //IBit[] allDependents = new IBit[] {} ;
        //Dictionary<IBit, bool> allPrevValues = new();
        //if (fwd.ContainsKey(bit))
        //{
        //    allDependents = collectForwards(bit).ToArray();
        //    allPrevValues = allDependents.ToDictionary(dep => dep, dep => dep.Value);
        //    Console.WriteLine();
        //}
        // } debug




        if (fwd.ContainsKey(bit))
        {
            //var dependents = fwd[bit]
            //    //.OfType<BitReEvaluatable>().ToArray()
            //    ;
            //var prevValues = dependents.ToDictionary(dep => dep, dep => dep.Value);
            var dependents = collectForwards(bit).ToArray();
            var prevValues = dependents.ToDictionary(dep => dep, dep => dep.Value);


            // 실제 변경 적용
            DoApply(bitChange);

            //// { debug
            //var changedxxx =
            //    (from kv in allPrevValues
            //     let b = kv.Key
            //     let val = kv.Value
            //     where val != b.Value
            //     select b
            //     ).ToArray();

            //var changedPortxx =
            //    changedxxx.OfType<PortInfo>().ToArray();
            //if (changedPortxx.Any())
            //    Console.WriteLine();
            //// } debug


            // 변경으로 인한 파생 변경 enqueue
            {
                bool getValue(IBit dep) => (dep is BitReEvaluatable re) ? re.Evaluate() : dep.Value;
                List<BitChange> changes = new();
                foreach(var dep in dependents)
                {
                    BitChange bc = null;
                    if (dep is PortInfo pi)
                    {
                        if (pi.Plan == bit)
                        {
                            if (pi.Actual == null || pi.Actual.Value == pi.Plan.Value)
                                bc = new BitChange(dep, bitChange.NewValue, bit, bitChange.OnError);
                            else
                                Console.WriteLine();
                        }
                        else if (pi.Actual == bit)
                        {
                            if (pi.Actual.Value == pi.Plan.Value)
                                bc = new BitChange(dep, bitChange.NewValue, bit, bitChange.OnError);
                            else
                                Console.WriteLine();
                        }
                    }

                    if (bc == null)
                    {
                        var newValue = getValue(dep);
                        if (newValue != prevValues[dep])
                            bc = new BitChange(dep, newValue, bit, bitChange.OnError);
                    }
                    if (bc != null)
                        changes.Add(bc);
                }

                if (cpu.FFSetterMap.ContainsKey(bit) && bitChange.NewValue)
                    foreach (var ff in cpu.FFSetterMap[bit].Where(ff => !ff.Value))
                        changes.Add(new BitChange(ff, true, bit, bitChange.OnError));

                if (cpu.FFResetterMap.ContainsKey(bit) && bitChange.NewValue)
                    foreach (var ff in cpu.FFResetterMap[bit].Where(ff => ff.Value))
                        changes.Add(new BitChange(ff, false, bit, bitChange.OnError));

                //var portChanges =
                //    dependents
                //        .OfType<PortInfo>()
                //        .Where(pi =>
                //        {
                //            if (pi.Plan == bit && pi.Actual == null)
                //            {
                //                var writablePlan = pi.Plan is IBitWritable;
                //                Debug.Assert(dependents.Contains(pi));
                //                var valueChanged = prevValues[pi] != pi.Value;

                //                var bcChanged = bit.Value != bitChange.NewValue;
                //                var changeContains = changes.Any(bc => bc.Bit == pi);
                //                var changesLength = changes.Count;
                //                Console.WriteLine();

                //                if (!writablePlan)
                //                    Console.WriteLine();
                //            }
                //            return true;
                //        })
                //        .ToArray()
                //        ;
                //if (portChanges.Any())
                //    Console.WriteLine();


                foreach (var bc in changes)
                    cpu.Apply(bc, withQueue);

                //var chgrp = changes.GroupByToDictionary(ch => ch.Bit is PortInfo);
                //if (chgrp.ContainsKey(false))
                //{
                //    var nonPorts = chgrp[false];
                //    foreach (var bc in nonPorts)
                //        cpu.Apply(bc, withQueue);
                //}
                //if (chgrp.ContainsKey(true))
                //{
                //    var ports = chgrp[true];
                //    foreach (var bc in ports)
                //    {
                //        var port = (PortInfo)bc.Bit;
                //        if (bit == port.Plan)
                //        {
                //            var newBc = new PortInfoPlanChange(bc) { Applied = true };
                //            if (withQueue)
                //                q.Enqueue(newBc);
                //            else
                //                cpu.Apply(newBc, withQueue);
                //        }
                //        else if (bit == port.Actual)
                //        {
                //            var newBc = new PortInfoActualChange(bc);
                //            if (withQueue)
                //                q.Enqueue(newBc);
                //            else
                //                cpu.Apply(newBc, withQueue);

                //        }
                //        else
                //            throw new Exception("ERROR");
                //    }
                //}
            }
        }
        else
        {
            //Global.Logger.Warn($"Failed to find dependency for {bit.GetName()}");
            DoApply(bitChange);
        }
        cpu.NestingLevel--;


        void DoApply(BitChange bitChange)
        {
            bitChange.BeforeAction?.Invoke();
            DoApplyBitChange(bitChange);
            bitChange.AfterAction?.Invoke();
        }

        void DoApplyBitChange(BitChange bitChange)
        {
            var bit = (Bit)bitChange.Bit;
            //Global.Logger.Debug($"\t=({indent}) Applying bitchange {bitChange}");

            // bit 가 나의 cpu 의 bit 가 아닌 경우, 타 cpu 에서 수행할 수 있도록 tag 변경을 공지.
            // e.g.  Call 의 TX 에 해당하는 bit 변경은 Call 이 정의된 system 의 cpu 에서 처리한다.
            if (! cpu.BitsMap.ContainsKey(bit.Name))
            {
                if (bit is Tag)
                    Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(bit.Name, bitChange.NewValue));
                else
                    throw new Exception("ERROR");
            }

            if (bitChange.NewValue && (bit.Name == "StartPort_A_F_Vm" || bit.Name == "StartPort_B_F_Vp"))
                Console.WriteLine();


            var bitChanged = bitChange switch
            {
                PortInfoPlanChange pic => new Func<bool>(() =>
                {
                    var pc = pic;
                    if (!pc.Applied)
                        if (pc.PortInfo.Plan is IBitWritable writable)
                        {
                            writable.SetValue(pc.NewValue);
                            Global.RawBitChangedSubject.OnNext(new BitChange(writable, pc.NewValue, $"Plan 변경: [{pc.PortInfo.Plan}]={pc.NewValue}"));
                        }

                        else
                            throw new Exception("ERROR");
                    return pc.PortInfo.PlanValueChanged(pc.NewValue);
                })(),

                PortInfoActualChange ac => ac.PortInfo.ActualValueChanged(ac.NewValue),

                _ => new Func<bool>(() =>
                {
                    //Debug.Assert(bit is not PortInfo || Global.IsInUnitTest);

                    if (bit is IBitWritable writable)
                    {
                        writable.SetValue(bitChange.NewValue);
                        return true;
                    }
                    else
                    {
                        Debug.Assert(bit.Value == bitChange.NewValue);
                        return bit is PortInfo;
                    }
                })(),
            };

            if (bitChanged)
            {
                try
                {
                    Global.RawBitChangedSubject.OnNext(bitChange);
                    if (bit is Tag tag)
                        Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(bit.Name, bitChange.NewValue));
                }
                catch (Exception ex)
                {
                    Global.Logger.Error(ex);
                    if (bitChange.OnError == null)
                        throw;
                    else
                        bitChange.OnError(ex);

                }
            }
        }

    }



    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    public static void Enqueue(this Cpu cpu, BitChange bitChange)
    {
        Debug.Assert(bitChange.Bit.Cpu == cpu);
        switch (bitChange.Bit)
        {
            case Expression _:
            case BitReEvaluatable re when re is not PortInfo:
                throw new Exception("ERROR: Expression can't be set!");
            default:
                cpu.Queue.Enqueue(bitChange);
                break;
        };
    }
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        Enqueue(cpu, new BitChange(bit, newValue, cause));
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue) =>
        Enqueue(cpu, new BitChange(bit, newValue, null));


    public static void SendChange(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        SendChange(cpu, new BitChange(bit, newValue, cause));
    public static void SendChange(this Cpu cpu, BitChange bitChange) =>
        cpu.Apply(bitChange, false);
    public static void PostChange(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        Enqueue(cpu, new BitChange(bit, newValue, cause));

    public static void BuildBitDependencies(this Cpu cpu)
    {
        Debug.Assert(cpu.ForwardDependancyMap.IsNullOrEmpty());
        Debug.Assert(cpu.BackwardDependancyMap.IsNullOrEmpty());

        cpu.BackwardDependancyMap = new();

        var bwd = cpu.BackwardDependancyMap;
        var fwd = cpu.ForwardDependancyMap;

        void addRelationship(IBit slave, IBit master)
        {
            if (!fwd.ContainsKey(slave))
                fwd[slave] = new HashSet<IBit>();
            fwd[slave].Add(master);

            if (!bwd.ContainsKey(master))
                bwd[master] = new HashSet<IBit>();
            bwd[master].Add(slave);
        }

        void addSubRelationship(IBit bit)
        {
            switch(bit)
            {
                case Flag:
                case Tag:
                    break;
                case FlipFlop ff:
                    addRelationship(ff.S, ff);
                    addSubRelationship(ff.S);
                    addRelationship(ff.R, ff);
                    addSubRelationship(ff.R);
                    break;
                case PortInfo pi:
                    foreach (var mb in pi._monitoringBits.Where(b => b != null))
                    {
                        addRelationship(mb, pi);
                        addSubRelationship(mb);
                    }
                    break;
                case BitReEvaluatable bre:
                    foreach (var mb in bre._monitoringBits.Where(b => b != null))
                    {
                        addRelationship(mb, bre);
                        addSubRelationship(mb);
                    }
                    break;
                default:
                    throw new Exception("ERROR");
            }
        }

        var grp = cpu.BitsMap.Values.GroupByToDictionary(b => b is BitReEvaluatable);
        if (grp.ContainsKey(true))
        {
            var stems = grp[true].Cast<BitReEvaluatable>();
            foreach (var stem in stems)
                addSubRelationship(stem);
        }

        var ffs = cpu.BitsMap.Values.OfType<FlipFlop>();
        foreach (var ff in ffs)
            addSubRelationship(ff);
    }
}
