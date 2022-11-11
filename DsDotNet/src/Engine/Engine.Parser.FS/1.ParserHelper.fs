namespace rec Engine.Parser.FS

open Engine.Core
open Engine.Common.FS
open System.Collections.Generic
open System.Linq

type AliasTarget() = class end

type AliasTargetWithFqdn(targetFqdn:Fqdn) =
    inherit AliasTarget()

    member val TargetFqdn = targetFqdn with get, set

type AliasTargetReal(targetFqdn:Fqdn) =
    inherit AliasTargetWithFqdn(targetFqdn)

type AliasTargetDirectCall(targetFqdn:Fqdn) =
    inherit AliasTargetWithFqdn(targetFqdn)


type AliasTargetApi(apiItem:ApiItem) =
    inherit AliasTarget()
    member val ApiItem = apiItem with get, set


type AliasCreator(name:string, parent:ParentWrapper, target:AliasTarget) =
    member val Name = name with get, set
    member val Parent = parent with get, set
    member val Target = target with get, set


[<AutoOpen>]
module internal ParserHelperModule =
    let tryFindSystem(fromSystem:DsSystem) (systemNames:Fqdn) =
        let rec helper (fromSystem:DsSystem) (systemNames:string list) =
            match systemNames with
            | n::[] when fromSystem.Name = n -> Some fromSystem
            | n::[] -> None
            | n::ns when fromSystem.Name = n ->
                let child = fromSystem.Systems.TryFind(fun s -> s.Name = n)
                match child with
                | Some child -> helper child ns
                | None -> None
            | _ -> None
        helper fromSystem (systemNames.ToFSharpList())


type ParserHelper(options:ParserOptions) =
    let mutable theSystem:DsSystem option = None
    member val Model = Model()
    member val ParserOptions = options with get, set

    /// 3.2.ElementListener 에서 Alias create 사용
    member val AliasCreators = ResizeArray<AliasCreator>()
    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    //member internal x._system = x._systems |> Seq.tryHead
    //member val internal _systems = Stack<DsSystem>()

    member x.TheSystem = theSystem.Value
    member internal x._theSystem
        with get() = theSystem
        and set(v) = theSystem <- v; x.Model.TheSystem <- v
    member val internal _currentSystem:DsSystem option = None with get, set

    member val internal _flow:Flow option = None  with get, set
    member val internal _parenting:Real option = None  with get, set
    member val internal _causalTokenElements = Dictionary<ContextInformation, GraphVertexType>(ContextInformation.CreateFullNameComparer())
    member val internal _elements = Dictionary<ContextInformation, GraphVertexType>()
    member val internal _modelSpits:SpitResult array = [||] with get, set
    member internal x._modelSpitObjects = x._modelSpits.Select(fun spit -> spit.GetCore()).ToArray()
    //member internal x.UpdateModelSpits() = x._modelSpits <- x.Model.Spit().ToArray()
    member internal x.UpdateModelSpits() =
        x._modelSpits <-
            [|
                match x._theSystem with
                | Some system -> yield! system.Spit()
                | _ -> ()
            |]

    member internal x.AppendPathElement(lastName:string) =
        x.CurrentPathElements.Append(lastName).ToArray()
    member internal x.AppendPathElement(lastNames:Fqdn) =
        x.CurrentPathElements.Concat(lastNames).ToArray()

    member internal x.CurrentPathElements with get():Fqdn =
        let helper() = [
            match x._currentSystem with
            | Some sys -> yield! sys.NameComponents // yield sys.Name
            | None -> ()
            match x._flow with
            | Some f -> yield f.Name
            | None -> ()
            match x._parenting with
            | Some f -> yield f.Name
            | None -> ()
        ]

        helper().ToArray()

    member internal x.CurrentPath with get() = x.CurrentPathElements.Combine()
    member x.TryFindSystem(systemNames:Fqdn) = tryFindSystem (x.TheSystem) systemNames
