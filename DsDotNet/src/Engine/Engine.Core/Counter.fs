namespace Engine.Core
open System
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Disposables
open Engine.Common.FS


(*
 - Timer 설정을 위한 조건: expression 으로 받음.
 - Timer statement 는 expression 을 매 scan 마다 평가.  값이 변경되면(rising or falling) 해당 timer 에 반영
 - Timer 가 설정되고 나면, observable timer 에 의해서 counter 값이 하나씩 감소하고, 0 이 되면 target trigger
*)

[<AutoOpen>]
module rec CounterModule =
    type CounterType =
        /// UP Counter
        CTU
        /// DOWN Counter
        | CTD
        /// UP/DOWN Counter
        | CTUD
        /// Ring Counter
        | CTR

    type CounterParams = {
        Type: CounterType
        Storages:Storages
        Name:string
        Preset: CountUnitType
        Accumulator: CountUnitType
        CU: TagBase<bool>
        CD: TagBase<bool>
        OV: TagBase<bool>
        UN: TagBase<bool>
        DN: TagBase<bool>
        /// XGI load
        LD: TagBase<bool>
        DNDown: TagBase<bool>

        RES: TagBase<bool>
        PRE: TagBase<CountUnitType>
        ACC: TagBase<CountUnitType>
    }

    let private CreateCounterParameters(typ:CounterType, storages:Storages, name, preset, accum:CountUnitType) =
        let nullB = getNull<TagBase<bool>>()
        let mutable cu  = nullB  // Count up enable bit
        let mutable cd  = nullB  // Count down enable bit
        let mutable ov  = nullB  // Overflow
        let mutable un  = nullB  // Underflow
        let mutable ld  = nullB  // XGI: Load

        let mutable dn  = nullB
        let mutable dnDown  = nullB
        let mutable pre = getNull<TagBase<CountUnitType>>()
        let mutable acc = getNull<TagBase<CountUnitType>>()
        let mutable res = nullB
        let add = addTagsToStorages storages
        match RuntimeTarget, typ with
        | XGI, CTU ->
            cu  <- fwdCreateBoolTag     $"{name}.CU" false  // Count up enable bit
            res <- fwdCreateBoolTag     $"{name}.R" false
            pre <- fwdCreateUShortTag   $"{name}.PV" preset
            dn  <- fwdCreateBoolTag     $"{name}.Q" false  // Done
            acc <- fwdCreateUShortTag   $"{name}.CV" accum
            add [cu; res; pre; dn; acc]

        | XGI, CTD ->
            cd  <- fwdCreateBoolTag     $"{name}.CD" false  // Count down enable bit
            ld  <- fwdCreateBoolTag     $"{name}.LD" false  // Load
            pre <- fwdCreateUShortTag   $"{name}.PV" preset
            dn  <- fwdCreateBoolTag     $"{name}.Q" false  // Done
            acc <- fwdCreateUShortTag   $"{name}.CV" accum
            add [cd; res; ld; pre; dn; acc]

        | XGI, CTUD ->
            cu  <- fwdCreateBoolTag     $"{name}.CU" false  // Count up enable bit
            cd  <- fwdCreateBoolTag     $"{name}.CD" false  // Count down enable bit
            res <- fwdCreateBoolTag     $"{name}.R" false
            ld  <- fwdCreateBoolTag     $"{name}.LD" false  // Load
            pre <- fwdCreateUShortTag   $"{name}.PV" preset
            dn  <- fwdCreateBoolTag     $"{name}.QU" false  // Done
            dnDown  <- fwdCreateBoolTag $"{name}.QD" false  // Done
            acc <- fwdCreateUShortTag   $"{name}.CV" accum
            add [cu; cd; res; ld; pre; dn; dnDown; acc]

        | XGI, CTR ->
            cd  <- fwdCreateBoolTag     $"{name}.CD" false  // Count down enable bit
            pre <- fwdCreateUShortTag   $"{name}.PV" preset
            res <- fwdCreateBoolTag     $"{name}.RST" false
            dn  <- fwdCreateBoolTag     $"{name}.Q" false  // Done
            acc <- fwdCreateUShortTag   $"{name}.CV" accum
            add [cd; pre; res; dn; acc]

        | _ ->
            match typ with
            | CTU ->
                cu  <- fwdCreateBoolTag     $"{name}.CU" false  // Count up enable bit
                add [cu]
            | CTR | CTD ->
                cd  <- fwdCreateBoolTag     $"{name}.CD" false  // Count down enable bit
                add [cd]
            | CTUD ->
                cu  <- fwdCreateBoolTag     $"{name}.CU" false  // Count up enable bit
                cd  <- fwdCreateBoolTag     $"{name}.CD" false  // Count down enable bit
                add [cu; cd]


            ov  <- fwdCreateBoolTag     $"{name}.OV" false  // Overflow
            un  <- fwdCreateBoolTag     $"{name}.UN" false  // Underflow
            ld  <- fwdCreateBoolTag     $"{name}.LD" false  // XGI: Load
            dn  <- fwdCreateBoolTag     $"{name}.DN" false  // Done
            pre <- fwdCreateUShortTag   $"{name}.PRE" preset
            acc <- fwdCreateUShortTag   $"{name}.ACC" accum
            res <- fwdCreateBoolTag     $"{name}.RES" false
            add [ov; un; dn; pre; acc; res;]

        (* 내부 structure 가 AB 기반이므로, 메모리 자체는 생성하되, storage 에 등록하지는 않는다. *)
        if isItNull(ov) then
            ov  <- fwdCreateBoolTag     $"{name}.OV" false
        if isItNull(un) then
            un  <- fwdCreateBoolTag     $"{name}.UN" false
        if isItNull(cu) then
            cu  <- fwdCreateBoolTag     $"{name}.CU" false
        if isItNull(cd) then
            cd  <- fwdCreateBoolTag     $"{name}.CD" false
        if isItNull(cd) then
            res  <- fwdCreateBoolTag     $"{name}.RES" false

        {
            Type        = typ
            Storages    = storages
            Name        = name
            Preset      = preset
            Accumulator = accum
            CU          = cu
            CD          = cd
            OV          = ov
            UN          = un
            DN          = dn
            DNDown      = dnDown
            LD          = ld
            RES         = res
            PRE         = pre
            ACC         = acc
        }




    [<AbstractClass>]
    type CounterBaseStruct(cp:CounterParams) =
        inherit TimerCounterBaseStruct(cp.Storages, cp.Name, cp.Preset, cp.Accumulator, cp.DN, cp.PRE, cp.ACC, cp.RES)

        member _.CU:TagBase<bool> = cp.CU  // Count up enable bit
        member _.CD:TagBase<bool> = cp.CD  // Count down enable bit
        member _.OV:TagBase<bool> = cp.OV  // Overflow
        member _.UN:TagBase<bool> = cp.UN  // Underflow
        member _.LD:TagBase<bool> = cp.LD  // Load (XGI)
        member _.Type = cp.Type


    type ICounter = interface end

    type ICTU =
        inherit ICounter
        abstract CU:TagBase<bool>

    type ICTD =
        inherit ICounter
        abstract CD:TagBase<bool>
        abstract LD:TagBase<bool>

    type ICTUD =
        inherit ICTU
        inherit ICTD

    type ICTR =
        inherit ICounter
        abstract CD:TagBase<bool>


    type CTUStruct private(counterParams:CounterParams) =
        inherit CounterBaseStruct(counterParams)
        member _.CU = base.CU
        interface ICTU with
            member x.CU = x.CU
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum)
            let cs = new CTUStruct(counterParams)
            storages.Add(name, cs)
            cs

    type CTDStruct private(counterParams:CounterParams) =
        inherit CounterBaseStruct(counterParams)
        member _.CD = base.CD
        interface ICTD with
            member x.CD = x.CD
            member x.LD = x.LD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum)
            let cs = new CTDStruct(counterParams)
            storages.Add(name, cs)
            cs

    type CTUDStruct private(counterParams:CounterParams) =
        inherit CounterBaseStruct(counterParams)
        member _.CU = base.CU
        member _.CD = base.CD
        interface ICTUD with
            member x.CU = x.CU
            member x.CD = x.CD
            member x.LD = x.LD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum)
            let cs = new CTUDStruct(counterParams)
            storages.Add(name, cs)
            cs

    type CTRStruct(counterParams:CounterParams ) =
        inherit CounterBaseStruct(counterParams)
        member _.RES = base.RES
        interface ICTR with
            member x.CD = x.CD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum)
            let cs = new CTRStruct(counterParams)
            storages.Add(name, cs)
            cs

    type internal CountAccumulator(counterType:CounterType, counterStruct:CounterBaseStruct) =
        let disposables = new CompositeDisposable()

        let cs = counterStruct
        let registerLoad() =
            let csd = box cs :?> ICTD       // CTD or CTUD 둘다 적용
            ValueSubject
                .Where(fun storage -> storage = csd.LD && csd.LD.Value)
                .Subscribe(fun storage ->
                    cs.ACC.Value <- cs.PRE.Value
            ) |> disposables.Add

        let registerCTU() =
            let csu = box cs :?> ICTU
            ValueSubject
                .Where(fun storage -> storage = csu.CU && csu.CU.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1us
                    if cs.ACC.Value >= cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add
        let registerCTD() =
            let csd = box cs :?> ICTD
            registerLoad()
            ValueSubject
                .Where(fun storage -> storage = csd.CD && csd.CD.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value - 1us
                    if cs.ACC.Value <= cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add

        let registerCTR() =
            let csr = box cs :?> ICTR
            ValueSubject
                .Where(fun storage -> storage = csr.CD && csr.CD.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1us
                    if cs.ACC.Value = cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
                    if cs.ACC.Value > cs.PRE.Value then
                        cs.ACC.Value <- 1us
                        cs.DN.Value <- false
            ) |> disposables.Add

        let registerReset() =
            ValueSubject
                .Where(fun storage -> storage = cs.RES && cs.RES.Value)
                .Subscribe(fun storage ->
                    tracefn "Counter reset requested"
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- 0us
                    cs.DN.Value <- false
                    cs.CU.Value <- false
                    cs.CD.Value <- false
                    cs.OV.Value <- false
                    cs.UN.Value <- false
            ) |> disposables.Add


        let clear() =
            cs.OV.Value <- false
            cs.UN.Value <- false
            cs.DN.Value <- false
            cs.CU.Value <- false
            cs.CD.Value <- false
            cs.ACC.Value <- 0us

        do
            clear()
            registerReset()
            match cs, counterType with
            | :? CTUStruct, CTU -> registerCTU()
            | :? CTRStruct, CTR -> registerCTR()
            | :? CTDStruct, CTD -> registerCTD()
            | :? CTUDStruct, CTUD -> registerCTU(); registerCTD();
            | _ -> failwith "ERROR"

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()


