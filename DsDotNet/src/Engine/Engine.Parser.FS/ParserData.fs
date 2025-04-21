namespace Engine.Parser.FS

open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module ParserDataModule =
    let createMemberVariable (name:string) (expr:IExpression) (comment:string option) : IVariable =
        let v = expr.BoxedEvaluatedValue
        let createParam () =
            {
                defaultStorageCreationParams(unbox v) (VariableTag.PcUserVariable|>int) with
                    Name=name; Comment=comment
            }

        match v.GetType().Name with
        | BOOL   -> new MemberVariable<bool>   (createParam())
        | CHAR   -> new MemberVariable<char>   (createParam())
        | FLOAT32-> new MemberVariable<single> (createParam())
        | FLOAT64-> new MemberVariable<double> (createParam())
        | INT16  -> new MemberVariable<int16>  (createParam())
        | INT32  -> new MemberVariable<int32>  (createParam())
        | INT64  -> new MemberVariable<int64>  (createParam())
        | INT8   -> new MemberVariable<int8>   (createParam())
        | STRING -> new MemberVariable<string> (createParam())
        | UINT16 -> new MemberVariable<uint16> (createParam())
        | UINT32 -> new MemberVariable<uint32> (createParam())
        | UINT64 -> new MemberVariable<uint64> (createParam())
        | UINT8  -> new MemberVariable<uint8>  (createParam())
        | _  -> failwithlog "ERROR"


    /// Parsing 과정에 필요한 데이터를 담고 있는 클래스
    type ParserData(target:HwCPU, storages: Storages, exprParser: exprParser option) =
        new() = ParserData(WINDOWS, Storages(), None)
        member x.TargetType:HwCPU = target
        member x.Storages: Storages = storages
        member x.ExprParser: exprParser option = exprParser
        /// UDT 선언.  type 선언
        member val UdtDecls = ResizeArray<UdtDecl>()
        /// UDT 정의.  UDT 변수 정의
        member val UdtDefs = ResizeArray<UdtDef>()

        /// Lambda 함수 정의
        member val LambdaDefs = ResizeArray<LambdaDecl>()

        /// Proc 정의
        member val ProcDefs = ResizeArray<ProcDecl>()

        member val TimerCounterInstances = HashSet<string>()

    type ParserData with
        member x.TryGetUdtDecl (typ:string) : UdtDecl option =
            x.UdtDecls.TryFind(fun d -> d.TypeName = typ)
        member x.IsUdtType (typ:string) =
            x.TryGetUdtDecl typ |> Option.isSome
        member x.IsUdtMemberVariable (name:string) =
            option {
                match name with
                | RegexPattern @"^(\w+)(\[\d+\])?\.(\w+)$" [instanceName; _; memberVar] ->
                    let! decl = x.UdtDefs |> filter (fun udt -> udt.VarName = instanceName) |> Seq.tryExactlyOne
                    let! matchingDecl = x.UdtDecls.TryFind(fun d -> d.TypeName = decl.TypeName)
                    return matchingDecl.Members |> Seq.exists (fun m -> m.Name = memberVar)
                | _ -> ()
            } |> Option.defaultValue false

        member x.IsTimerOrCounterMemberVariable (name:string) =
            match name with
            | RegexPattern @"^(\w+)\.(\w+)$" [instanceName; _memberVar] ->
                x.TimerCounterInstances.Contains instanceName
            | _ -> false

        /// e.g "people[0].name" => typeof<string>
        member x.TryGetMemberVariableDataType (name:string) : System.Type option =
            assert (x.IsUdtMemberVariable name)
            option {
                match name with
                | RegexPattern @"^(\w+)(\[\d+\])?\.(\w+)$" [instanceName; _; memberVar] ->
                    let! decl = x.UdtDefs |> filter (fun udt -> udt.VarName = instanceName) |> Seq.tryExactlyOne
                    let! matchingDecl = x.UdtDecls.TryFind(fun d -> d.TypeName = decl.TypeName)
                    let! matchingMember = matchingDecl.Members |> filter (fun m -> m.Name = memberVar) |> Seq.tryExactlyOne
                    return matchingMember.Type
                | _ ->
                    ()
            }

        /// UDT 변수 => UDT Type.  e.g {"kim", "people[0]", or "people[1].name"} => Person
        member x.TryGetUdtTypeName (name:string) : string option =
            match name with
                | RegexPattern @"^(\w+)(\[\d+\])?(\.\w+)?$" [instanceName; _; _] ->
                    x.UdtDefs |> filter (fun udt -> udt.VarName = instanceName) |> Seq.tryExactlyOne |> Option.map (fun udt -> udt.TypeName)
                | _ -> None

        member x.AddUdtDefs (udtDef:UdtDef) =
            x.UdtDefs.Add(udtDef)

            /// UDT 변수 정의 부분을 풀어서 실제 변수(MemberVariable<T> type)들을 생성해서 storage 에 추가한다.
            /// PC 버젼에서는 PLC 와 달리, 실제 UDT 변수들을 필요로 한다.
            /// Person hong; => string hong.name, int hong.age 와 같이 생성해서 storage 에 추가한다.
            let { TypeName=typeName; VarName=varName; ArraySize=arraySize } = udtDef
            let udtDecl = x.TryGetUdtDecl(typeName).Value
            for i in 0..arraySize-1 do
                let arrIndex = if arraySize > 1 then $"[{i}]" else ""
                for m in udtDecl.Members do
                    let name = $"{varName}{arrIndex}.{m.Name}"
                    let exp = m.Type |> typeDefaultValue |> any2expr
                    let v = createMemberVariable name exp None
                    x.Storages[name] <- v
