// https://github.com/fsprojects/Argu
#r "nuget: Argu" 

open Argu

type CliArguments =
    | Working_Directory of path:string
    | Listener of host:string * port:int
    | Data of base64:byte[]
    | Port of tcp_port:int
    | Log_Level of level:int
    | Detach

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Working_Directory _ -> "specify a working directory."     // "--working-directory"
            | Listener _ -> "specify a listener (hostname : port)."
            | Data _ -> "binary data in base64 encoding."
            | Port _ -> "specify a primary port."
            | Log_Level _ -> "set the log level."   // "--log-level"
            | Detach _ -> "detach daemon from console."


let parser = ArgumentParser.Create<CliArguments>(programName = "gadget.exe")
let usage = parser.PrintUsage()
printfn "%s" usage



//let results = parser.Parse [| "--detach" ; "--listener" ; "localhost" ; "8080" |]
let results = parser.Parse [|
    "--log-level"; "1";
    "--working-directory"; "c:\\kwak";
    "--detach" ;
    "--listener" ; "localhost" ; "8080" |]
let all = results.GetAllResults() // [ Detach ; Listener ("localhost", 8080) ]


let detach = results.Contains Detach
let listener = results.GetResults Listener


let dataOpt = results.TryGetResult Data
let logLevel = results.GetResult (Log_Level, defaultValue = 0)

