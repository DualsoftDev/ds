

using DevExpress.CodeParser;

namespace DSModeler.Event;
[SupportedOSPlatform("windows")]
public static class EventCPU
{
    private static IDisposable DisposableHWDSInput;
    private static IDisposable DisposableTagDS;

    public static void CPUSubscribe()
    {
        if (DisposableHWDSInput == null && Global.CpuRunMode.IsPackagePC())
        {
            DisposableHWDSInput = Global.DsDriver.Conn.Subject.OfType<TagValueChangedEvent>()
            .Subscribe(evt =>
            {
                TagHW t = evt.Tag as TagHW;
                if (t.IOType == TagIOType.Output)
                {
                    Global.Logger.Debug($"HW_OUT {t.Name}({t.Address}) value: {t.Value}");
                }
                if (t.IOType == TagIOType.Input)
                {
                    var tags = PcContr.DicActionIn[t];
                    tags.Iter(tag =>
                    {
                        tag.BoxedValue = t.Value;
                        if (tag.Target.Value is TaskDev dev && ViewDraw.DicTask.ContainsKey(dev)) //job만정의 하고 call에 사용  안함
                        {
                            IEnumerable<Vertex> vs = ViewDraw.DicTask[dev];
                            _ = vs.Iter(v => ViewDraw.ActionChangeSubject
                                               .OnNext(Tuple.Create(v, t.Value)));
                        }
                    });
                    Global.Logger.Debug($"HW_IN {t.Name}({t.Address}) value: {t.Value}");
                }
            });
        }

        DisposableTagDS ??=
                TagDSSubject
                .Subscribe(evt =>
                {
                    if (evt.IsEventVertex)
                    {
                        EventVertex t = evt as EventVertex;
                        bool isStatus = t.TagKind == VertexTag.ready
                                      || t.TagKind == VertexTag.going
                                      || t.TagKind == VertexTag.finish
                                      || t.TagKind == VertexTag.homing;

                    

                        if (isStatus && (bool)t.Tag.BoxedValue)
                        {
                            ViewDraw.StatusChangeSubject.OnNext(t);
                        }

                        if (Global.CpuRunMode.IsSimulation)
                        {
                            Task.Delay(ControlProperty.GetDelayMsec()).Wait();
                        }
                        else
                        {
                            _ = Task.Yield();
                        }
                    }
                    else if (evt.IsEventAction && Global.CpuRunMode.IsPackagePC())
                    {
                        Tag<bool> tag = (evt as EventAction).Tag as Tag<bool>;
                        if (PcContr.DicActionOut.ContainsKey(tag))
                        {
                            var tagHW = PcContr.DicActionOut[tag];

                            if (Global.DSHW.Company == Company.LSE)
                                ((XG5KTag)tagHW).XgPLCTag.WriteValue = tag.Value;
                            else
                                tagHW.WriteRequestValue = tag.Value;


                        }

                    }

                    LogicLog.AddLogicLog(evt);
                });

    }



    public static void CPUUnsubscribe()
    {
        DisposableHWDSInput?.Dispose();
        DisposableHWDSInput = null;
        DisposableTagDS?.Dispose();
        DisposableTagDS = null;
    }

}

