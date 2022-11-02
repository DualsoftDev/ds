namespace Engine.Parser.FS

open Engine.Core
open System.Collections.Generic
open System.Linq

type AliasTarget() = class end

type AliasTargetWithFqdn(targetFqdn:NameComponents) =
    inherit AliasTarget()

    member val TargetFqdn = targetFqdn with get, set

type AliasTargetReal(targetFqdn:NameComponents) =
    inherit AliasTargetWithFqdn(targetFqdn)

type AliasTargetDirectCall(targetFqdn:NameComponents) =
    inherit AliasTargetWithFqdn(targetFqdn)


type AliasTargetApi(apiItem:ApiItem) =
    inherit AliasTarget()
    member val ApiItem = apiItem with get, set


type AliasCreator(name:string, parent:ParentWrapper, target:AliasTarget) =
    member val Name = name with get, set
    member val Parent = parent with get, set
    member val Target = target with get, set

type ParserHelper(options:ParserOptions) =
    member val Model = Model()
    member val ParserOptions = options with get, set

    /// 3.2.ElementListener 에서 Alias create 사용
    member val AliasCreators = ResizeArray<AliasCreator>()
    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val internal _system:DsSystem option = None with get, set
    member val internal _flow:Flow option = None  with get, set
    member val internal _parenting:Real option = None  with get, set
    member val internal _elements = Dictionary<string[], GraphVertexType>(NameUtil.CreateNameComponentsComparer())
    member val internal _modelSpits:SpitResult array = [||] with get, set
    member val internal _modelSpitObjects:obj array = [||] with get, set

    member internal x.AppendPathElement(lastName:string) =
        x.CurrentPathElements.Append(lastName).ToArray()
    member internal x.AppendPathElement(lastNames:string[]) =
        x.CurrentPathElements.Concat(lastNames).ToArray()

    member internal x.CurrentPathElements with get():string[] =
        let helper() = [
            match x._system with
            | Some sys -> yield sys.Name
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

