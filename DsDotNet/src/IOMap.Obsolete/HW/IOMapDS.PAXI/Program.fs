
[<EntryPoint>]
let main argv = 
    let ip, cpu = 
        if argv.Length = 0
        then "192.168.0.12", "NMC2"
        else argv[0], argv[1]

    let scanIO = IOMap.LS.ScanImpl.ScanIO(ip, cpu)

    while true do
        scanIO.DoScan()    
        
    0  

