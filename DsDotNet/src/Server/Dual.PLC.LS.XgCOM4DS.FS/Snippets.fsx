
module Snippets

let testme() =
    //for i in [0..63] do
    //    let di = conn.Factory.CreateDevice()
    //    di.ucDeviceType <- Convert.ToByte('M')
    //    di.ucDataType <- Convert.ToByte('L')
    //    di.lSize <- 64
    //    di.lOffset <- i*64

    (* byte 단위 읽기 *)
    //for i in [64..127] do
    //    let di = conn.Factory.CreateDevice()
    //    di.ucDeviceType <- Convert.ToByte('M')
    //    di.ucDataType <- Convert.ToByte('B')
    //    di.lSize <- 1
    //    di.lOffset <- i
    let xxx = [1..2..63]
    for i in [0..100..6300] do
        let di = conn.CreateMLWordDevice i
        conn.CommObject.AddDeviceInfo di
    let rBuf = Array.zeroCreate<byte>(1024)      // + rBuf는 하나만 만든다. list의 array를 돌아가면서 읽고 비교하고 덮어쓴다.
    rBuf.[0] <- 0xabuy
    if conn.CommObject.ReadRandomDevice rBuf <> 1 then
        if not (conn.CheckConnect()) then 
            failwith "Connection Failed"
    let xxx = rBuf |> Array.filter (fun x -> x <> 0uy)
    ()


type PLCMonitorEngine() =
    member x.Test() =
        let tags = [ 1; 3; 9; 31; 33; 65; 255; 1025; ] @ [ for i in [1..66] -> i*2*64+1 ]
                   |> List.map (sprintf "%%MX%d")
        //let tags = [ "%MX0"; "%MX1"; "%MX128"; "%MX129"; "%MX7553"; "%MX7554"; "%MX7555" ; "%MX7580"; "%MX7655"]

        x.Monitor("192.168.0.100:2004", tags)


    member x.Monitor(plcIp, tags:string seq) =
        let testSubscription =
            x.PLCTagChangedSubject.Subscribe(fun ci ->
                Trace.WriteLine($"{ci.Tag} => {ci.Value}");
            )
        //x.PLCTagChangedSubject.OnNext(ChangedTagInfo("", "", null))



