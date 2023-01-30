open System

let (|ToUppper|) (inp:string) = inp.ToUpper()
let printUpper (ToUppper x) = printfn "%s" x
printUpper "hello"


open System.IO
let (|KBSize|MBSize|GBSize|) filePath =
    let s = FileInfo(filePath).Length
    let kb = 1024L
    let mb = kb * kb
    let gb = mb * kb
    if s < mb then
        KBSize
    elif s < gb then
        MBSize
    else
        GBSize

let (|EndsWithExtension|) ext filePath =
    Path.GetExtension(filePath) = ext

let isImage (EndsWithExtension ".jpg" filePath) =
    filePath

let (|IsImageFile|) filePath = isImage filePath

let a =
    let f = @"C:\Users\Public\Documents\DevExpress Demos 18.1\Components\Data\AccordionControlData\(Athens)-Academy-of-Athens.jpg" 
    match f with
    | IsImageFile bImageFile ->
        printfn "%s is %s file" f (if bImageFile then "image" else "not image")
