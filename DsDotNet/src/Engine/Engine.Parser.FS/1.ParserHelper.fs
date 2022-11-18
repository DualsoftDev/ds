namespace Engine.Parser.FS

open Engine.Core
open System.Collections.Generic
open System.Linq
open Antlr4.Runtime
open type Engine.Parser.dsParser

[<AbstractClass>]
type AliasTargetBase() = class end

type AliasTargetWithFqdn(targetFqdn:Fqdn) =
    inherit AliasTargetBase()

    member val TargetFqdn = targetFqdn with get, set

type AliasTargetReal(targetFqdn:Fqdn) =
    inherit AliasTargetWithFqdn(targetFqdn)

type AliasTargetDirectCall(targetFqdn:Fqdn) =
    inherit AliasTargetWithFqdn(targetFqdn)


type AliasTargetApi(apiItem:ApiItem4Export) =
    inherit AliasTargetBase()
    member val ApiItem4Export = apiItem with get, set


type AliasCreator(name:string, parent:ParentWrapper, target:AliasTargetBase) =
    member val Name = name with get, set
    member val Parent = parent with get, set
    member val Target = target with get, set


type ParserHelper(options:ParserOptions) =
    member val ParserOptions = options with get, set

    /// 3.2.ElementListener 에서 Alias create 사용
    member val AliasCreators = ResizeArray<AliasCreator>()
    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val TheSystem:DsSystem option = None with get, set

    member val internal _flow:Flow option = None  with get, set
    member val internal _parenting:Real option = None  with get, set


    member val internal _aliasListingContexts        = ResizeArray<AliasListingContext>()
    member val internal _callListingContexts         = ResizeArray<CallListingContext>()
    member val internal _parentingBlockContexts      = ResizeArray<ParentingBlockContext>()

    member val internal _causalPhraseContexts        = ResizeArray<CausalPhraseContext>()
    member val internal _causalTokenContext          = ResizeArray<CausalTokenContext>()
    member val internal _identifier12ListingContexts = ResizeArray<Identifier12ListingContext>()

    member val internal _deviceBlockContexts         = ResizeArray<LoadDeviceBlockContext>()
    member val internal _externalSystemBlockContexts = ResizeArray<LoadExternalSystemBlockContext>()


    member val internal _modelSpits:SpitResult array = [||] with get, set
    member internal x._modelSpitObjects = x._modelSpits.Select(fun spit -> spit.GetCore()).ToArray()
    member internal x.UpdateModelSpits() =
        x._modelSpits <-
            [|
                match x.TheSystem with
                | Some system -> yield! system.Spit()
                | _ -> ()
            |]

