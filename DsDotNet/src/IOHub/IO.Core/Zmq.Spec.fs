namespace IO.Core

[<AutoOpen>]
module ZmqSpec =
    type ReadResult(error:string, result:obj) =
        member val Error = error with get, set
        member val Result = result  with get, set

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


