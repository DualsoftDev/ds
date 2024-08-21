#r "nuget: Newtonsoft.Json"
#r "nuget: NetMQ"
#r "nuget: Dualsoft-Common-Core-FS"
#r "../IO.Core/bin/Debug/net8.0/IO.Core.dll"

open System
open IO.Core

let port = 5555
let client = new Zmq.Client($"tcp://localhost:{port}")
let rr0 = client.SendRequest("read Mw100 Mx30 Md1234")
let result = client.SendRequest("read Mw100 Mx30")
let result2 = client.SendRequest("read Mw100 Mb70 Mx30 Md50 Ml50")
//let result3 = client.SendRequest("read [Mw100..Mw30]")
let wr = client.SendRequest("write Mw100=1 Mx30=false Md1234=1234")
let rr = client.SendRequest("read Mw100 Mx30 Md1234")
