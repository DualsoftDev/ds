namespace IO.Core
open System

[<AutoOpen>]
module ZmqSpec =
    type IIOResult = interface end
    type IIOResultOK = inherit IIOResult
    type IIOResultNG =
        inherit IIOResult
        abstract member Error:string

    [<AbstractClass>]
    type IOResult(error:string) =
        member x.Error = error
        member x.IsOK = String.IsNullOrEmpty(error)
        interface IIOResult

    [<AbstractClass>]
    type ReadResult(error:string) =
        inherit IOResult(error)

    type ReadResultArray<'T>(results:'T[]) =
        inherit ReadResult(null)
        interface IIOResultOK
        member val Results = results

    type ReadResultString(result:string) =
        inherit ReadResult(null)
        interface IIOResultOK
        member val Result = result

    type ReadResultError(error:string) =
        inherit ReadResult(error)
        interface IIOResultNG with
            member x.Error = error

    type WriteResultOK() =
        inherit IOResult(null)
        interface IIOResultOK
    type WriteResultError(error:string) =
        inherit IOResult(error)
        interface IIOResultNG with
            member x.Error = error




    /// MW100 : name='M', type='W', offset=100.  (MX30, MD1234, ML1234, ..)
    type AddressSpec(name:string, typ:string, offset:int) =
        member val Name = name.ToLower() with get, set
        member val Offset = offset with get, set
        member val Type = typ.ToLower() with get, set

    type ByteRange(s, e) = 
        member val Start = s with get, set
        member val End = e with get, set
    type IOFileSpec(name:string, length:int, validRanges:ByteRange[]) =
        member val Name = name.ToLower()  with get, set
        member val Length = length with get, set
        member val ValidRanges:ByteRange[] = validRanges with get, set
    type IOSpec(servicePort:int, files:IOFileSpec[]) =
        member val Location = "." with get, set
        member val ServicePort = servicePort with get, set
        member val Files = files with get, set


