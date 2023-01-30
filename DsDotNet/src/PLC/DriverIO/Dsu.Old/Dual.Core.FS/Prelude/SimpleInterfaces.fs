namespace Old.Dual.Core.Prelude

open Old.Dual.Common


open System
open System.Collections.Generic
/// Node base interface
type INode =
    abstract member Parent:INodeStem option with get, set
and
    /// Stem.  파일시스템에 비유하면 폴더에 해당
    INodeStem =
        inherit INode
        abstract member Children:INode seq with get, set
/// Leaf.  파일시스템에 비유하면 파일에 해당
type INodeLeaf =
    inherit INode

type IDsFile    = interface end
type IDsProject = interface end
type IDsArea    = interface end
type IDsDevice  = interface end
type IWork      = interface end


