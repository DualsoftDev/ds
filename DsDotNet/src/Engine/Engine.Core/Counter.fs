namespace Engine.Core
open System
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Disposables
open Dual.Common.Core.FS


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
        CU: VariableBase<bool>
        CD: VariableBase<bool>
        OV: VariableBase<bool>
        UN: VariableBase<bool>
        DN: VariableBase<bool>
        /// XGI load
        LD: VariableBase<bool>
        DNDown: VariableBase<bool>

        RES: VariableBase<bool>
        PRE: VariableBase<CountUnitType>
        ACC: VariableBase<CountUnitType>
    }

    let private CreateCounterParameters(typ:CounterType, storages:Storages, name, preset, accum:CountUnitType, target:PlatformTarget) =
        let nullB = getNull<VariableBase<bool>>()
        let mutable cu  = nullB  // Count up enable bit
        let mutable cd  = nullB  // Count down enable bit
        let mutable ov  = nullB  // Overflow
        let mutable un  = nullB  // Underflow
        let mutable ld  = nullB  // XGI: Load

        let mutable dn  = nullB
        let mutable dnDown  = nullB
        let mutable pre = getNull<VariableBase<CountUnitType>>()
        let mutable acc = getNull<VariableBase<CountUnitType>>()
        let mutable res = nullB
        let add = addTagsToStorages storages
        let dnName = if target = XGK
                     then $"{name}{xgkTimerCounterContactMarking}"
                     else
                        if typ = CTUD
                        then $"{name}.QU"
                        else $"{name}.Q"

        match target, typ with
        | (WINDOWS | XGI| XGK), CTU ->
            cu  <- createBool     $"{name}.CU" false  // Count up enable bit
            res <- createBool     $"{name}.R" false
            pre <- createUInt32   $"{name}.PV" preset
            dn  <- createBoolWithTagKind  dnName false  (VariableTag.PcSysVariable|>int) // Done
            acc <- createUInt32   $"{name}.CV" accum
            add [cu; res; pre; dn; acc]

        | (WINDOWS | XGI| XGK), CTD ->
            cd  <- createBool     $"{name}.CD" false   // Count down enable bit
            ld  <- createBool     $"{name}.LD" false   // Load
            pre <- createUInt32   $"{name}.PV" preset
            dn  <- createBoolWithTagKind    dnName false  (VariableTag.PcSysVariable|>int) // Done
            acc <- createUInt32   $"{name}.CV" accum
            add [cd; res; ld; pre; dn; acc]

        | (WINDOWS | XGI| XGK), CTUD ->
            cu  <- createBool     $"{name}.CU" false  // Count up enable bit
            cd  <- createBool     $"{name}.CD" false  // Count down enable bit
            res <- createBool     $"{name}.R" false
            ld  <- createBool     $"{name}.LD" false  // Load
            pre <- createUInt32   $"{name}.PV" preset
            dn  <- createBoolWithTagKind     dnName false   (VariableTag.PcSysVariable|>int) // Done
            dnDown  <- createBoolWithTagKind $"{name}.QD" false   (VariableTag.PcSysVariable|>int) // Done
            acc <- createUInt32   $"{name}.CV" accum
            add [cu; cd; res; ld; pre; dn; dnDown; acc]

        | (WINDOWS | XGI| XGK), CTR ->
            cd  <- createBool     $"{name}.CD" false   // Count down enable bit
            pre <- createUInt32   $"{name}.PV" preset
            res <- createBool     $"{name}.RST" false
            dn  <- createBoolWithTagKind     dnName false    (VariableTag.PcSysVariable|>int) // Done
            acc <- createUInt32   $"{name}.CV" accum
            add [cd; pre; res; dn; acc]

        | _ ->
            match typ with
            | CTU ->
                cu  <- createBool     $"{name}.CU" false  // Count up enable bit
                add [cu]
            | CTR | CTD ->
                cd  <- createBool     $"{name}.CD" false  // Count down enable bit
                add [cd]
            | CTUD ->
                cu  <- createBool     $"{name}.CU" false // Count up enable bit
                cd  <- createBool     $"{name}.CD" false // Count down enable bit
                add [cu; cd]


            ov  <- createBool     $"{name}.OV" false   // Overflow
            un  <- createBool     $"{name}.UN" false   // Underflow
            ld  <- createBool     $"{name}.LD" false   // XGI: Load
            dn  <- createBoolWithTagKind     $"{name}.DN" false  (VariableTag.PcSysVariable|>int) // Done
            pre <- createUInt32   $"{name}.PRE" preset
            acc <- createUInt32   $"{name}.ACC" accum
            res <- createBool     $"{name}.RES" false
            add [ov; un; dn; pre; acc; res;]

        (* 내부 structure 가 AB 기반이므로, 메모리 자체는 생성하되, storage 에 등록하지는 않는다. *)
        if isItNull(ov) then
            ov  <- createBool     $"{name}.OV" false
        if isItNull(un) then
            un  <- createBool     $"{name}.UN" false
        if isItNull(cu) then
            cu  <- createBool     $"{name}.CU" false
        if isItNull(cd) then
            cd  <- createBool     $"{name}.CD" false
        if isItNull(cd) then
            res  <- createBool     $"{name}.RES" false

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
    type CounterBaseStruct(cp:CounterParams, sys) =
        inherit TimerCounterBaseStruct(Some false, cp.Name, cp.DN, cp.PRE, cp.ACC, cp.RES, sys)

        member _.CU:VariableBase<bool> = cp.CU  // Count up enable bit
        member _.CD:VariableBase<bool> = cp.CD  // Count down enable bit
        member _.OV:VariableBase<bool> = cp.OV  // Overflow
        member _.UN:VariableBase<bool> = cp.UN  // Underflow
        member _.LD:VariableBase<bool> = cp.LD  // Load (XGI)
        member _.Type = cp.Type
        override x.ResetStruct() =
            base.ResetStruct()
            clearVarBoolsOnDemand([x.OV; x.UN; x.CU; x.CD;]);

    type ICounter = interface end

    type ICTU =
        inherit ICounter
        abstract CU:VariableBase<bool>

    type ICTD =
        inherit ICounter
        abstract CD:VariableBase<bool>
        abstract LD:VariableBase<bool>

    type ICTUD =
        inherit ICTU
        inherit ICTD

    type ICTR =
        inherit ICounter
        abstract CD:VariableBase<bool>


    type CTUStruct private(counterParams:CounterParams, sys) =
        inherit CounterBaseStruct(counterParams, sys)
        member _.CU = base.CU
        interface ICTU with
            member x.CU = x.CU
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType, sys, target:PlatformTarget) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum, target)
            let cs = new CTUStruct(counterParams, sys)
            storages.Add(name, cs)
            cs

    type CTDStruct private(counterParams:CounterParams, sys) =
        inherit CounterBaseStruct(counterParams, sys)
        member _.CD = base.CD
        interface ICTD with
            member x.CD = x.CD
            member x.LD = x.LD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType, sys, target:PlatformTarget) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum, target)
            let cs = new CTDStruct(counterParams, sys)
            storages.Add(name, cs)
            cs

    type CTUDStruct private(counterParams:CounterParams, sys) =
        inherit CounterBaseStruct(counterParams, sys)
        member _.CU = base.CU
        member _.CD = base.CD
        interface ICTUD with
            member x.CU = x.CU
            member x.CD = x.CD
            member x.LD = x.LD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType, sys, target:PlatformTarget) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum, target)
            let cs = new CTUDStruct(counterParams, sys)
            storages.Add(name, cs)
            cs

    type CTRStruct(counterParams:CounterParams , sys) =
        inherit CounterBaseStruct(counterParams, sys)
        member _.RES = base.RES
        interface ICTR with
            member x.CD = x.CD
        static member Create(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType, sys, target:PlatformTarget) =
            let counterParams = CreateCounterParameters(typ, storages, name, preset, accum, target)
            let cs = new CTRStruct(counterParams, sys)
            storages.Add(name, cs)
            cs

    type internal CountAccumulator(counterType:CounterType, counterStruct:CounterBaseStruct)=
        let disposables = new CompositeDisposable()

        let cs = counterStruct
        let system = (counterStruct:>IStorage).DsSystem
        let registerLoad() =
            let csd = box cs :?> ICTD       // CTD or CTUD 둘다 적용
            CpusEvent.ValueSubject
                .Where(fun (sys, _storage, _value) -> sys = system)
                .Where(fun (_sys, storage, _newValue) -> storage = csd.LD && csd.LD.Value)
                .Subscribe(fun (_sys, _storage, _newValue) ->
                    cs.ACC.Value <- cs.PRE.Value
            ) |> disposables.Add

        let registerCTU() =
            let csu = box cs :?> ICTU
            CpusEvent.ValueSubject
                .Where(fun (sys, _storage, _value) -> sys = system)
                .Where(fun (_sys, storage, _newValue) -> storage = csu.CU && csu.CU.Value)
                .Subscribe(fun (_sys, _storage, _newValue) ->
                    if cs.ACC.Value < 0u || cs.PRE.Value < 0u then failwithlog "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1u
                    if cs.ACC.Value >= cs.PRE.Value then
                        debugfn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add
        let registerCTD() =
            let csd = box cs :?> ICTD
            registerLoad()
            CpusEvent.ValueSubject
                .Where(fun (sys, _storage, _value) -> sys = system)
                .Where(fun (_sys, storage, _newValue) -> storage = csd.CD && csd.CD.Value)
                .Subscribe(fun (_sys, _storage, _newValue) ->
                    if cs.ACC.Value < 0u || cs.PRE.Value < 0u then failwithlog "ERROR"
                    cs.ACC.Value <- cs.ACC.Value - 1u
                    if cs.ACC.Value <= cs.PRE.Value then
                        debugfn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add

        let registerCTR() =
            let csr = box cs :?> ICTR
            CpusEvent.ValueSubject
                .Where(fun (sys, _storage, _value) -> sys = system)
                .Where(fun (_sys, storage, _newValue) -> storage = csr.CD && csr.CD.Value)
                .Subscribe(fun (_sys, _storage, _newValue) ->
                    if cs.ACC.Value < 0u || cs.PRE.Value < 0u then failwithlog "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1u
                    if cs.ACC.Value = cs.PRE.Value then
                        debugfn "Counter accumulator value reached"
                        cs.DN.Value <- true
                    if cs.ACC.Value > cs.PRE.Value then
                        cs.ACC.Value <- 1u
                        cs.DN.Value <- false
            ) |> disposables.Add


        let registerReset() =
            CpusEvent.ValueSubject
                .Where(fun (sys, _storage, _value) -> sys = (counterStruct:>IStorage).DsSystem)
                .Where(fun (_sys, storage, _newValue) -> storage = cs.RES && cs.RES.Value)
                .Subscribe(fun (_sys, _storage, _newValue) ->
                    debugfn "Counter reset requested"
                    if cs.ACC.Value < 0u || cs.PRE.Value < 0u then
                        failwithlog "ERROR"
                    cs.ACC.Value <- 0u
                    clearVarBoolsOnDemand( [cs.DN; cs.CU; cs.CD; cs.OV; cs.UN;] )
            ) |> disposables.Add

        do
            cs.ResetStruct()
            registerReset()
            match cs, counterType with
            | :? CTUStruct, CTU -> registerCTU()
            | :? CTRStruct, CTR -> registerCTR()
            | :? CTDStruct, CTD -> registerCTD()
            | :? CTUDStruct, CTUD -> registerCTU(); registerCTD();
            | _ -> failwithlog "ERROR"

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()


