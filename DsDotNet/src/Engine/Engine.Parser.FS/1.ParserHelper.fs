namespace Engine.Parser.FS

open Engine.Core
open System.Collections.Generic
open type Engine.Parser.dsParser
open Engine.Common.FS

type ParserHelper(options:ParserOptions) =
    member val ParserOptions = options with get, set

    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val TheSystem:DsSystem = getNull<DsSystem>() with get, set

    member val internal _flow = getNull<Flow>() with get, set
    member val internal _parenting = getNull<Real>()  with get, set


    member val internal _aliasListingContexts        = ResizeArray<AliasListingContext>()
    member val internal _callListingContexts         = ResizeArray<CallListingContext>()
    member val internal _parentingBlockContexts      = ResizeArray<ParentingBlockContext>()

    member val internal _causalPhraseContexts        = ResizeArray<CausalPhraseContext>()
    member val internal _causalTokenContext          = ResizeArray<CausalTokenContext>()
    member val internal _identifier12ListingContexts = ResizeArray<Identifier12ListingContext>()


    member val internal _interfaceDefContexts = ResizeArray<InterfaceDefContext>()



    member val internal _deviceBlockContexts         = ResizeArray<LoadDeviceBlockContext>()
    member val internal _externalSystemBlockContexts = ResizeArray<LoadExternalSystemBlockContext>()

