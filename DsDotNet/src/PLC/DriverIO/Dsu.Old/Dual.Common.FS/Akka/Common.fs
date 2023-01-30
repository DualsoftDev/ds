namespace Old.Dual.Common.Akka

//open System.Net
//open Akka
open System.Net.NetworkInformation
open System.Net.Sockets

// Old.Dual.Common.FS 와의 dependancy 를 줄이기 위한 local copy version
module internal Common =
    /// Default argument 를 위한 active pattern
    // https://stackoverflow.com/questions/30255271/how-to-set-default-argument-value-in-f
    let inline (|Default|) defaultValue input =
        defaultArg input defaultValue

    /// get default arguments.
    let (|?) = defaultArg

    // https://stackoverflow.com/questions/8089685/c-sharp-finding-my-machines-local-ip-address-and-not-the-vms/25553311
    /// local system 의 IP addresses(복수개) 를 반환한다.
    ///
    /// VM ware 등에서 사용하는 가상 ip 주소를 제외한 실제 ip 주소(복수개) 만을 반환
    let getPhysicalIPAdresses() =
        [
            for ni in NetworkInterface.GetAllNetworkInterfaces() do
            for addr in ni.GetIPProperties().GatewayAddresses do
                if (addr <> null && addr.Address.ToString() <> "0.0.0.0") then
                    if (ni.NetworkInterfaceType = NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType = NetworkInterfaceType.Ethernet) then
                        for ip in ni.GetIPProperties().UnicastAddresses do
                            if (ip.Address.AddressFamily = AddressFamily.InterNetwork) then
                                yield ip.Address.ToString()
        ]
        

    /// local system 의 IP address를 반환한다.
    ///
    /// VM ware 등에서 사용하는 가상 ip 주소를 제외한 실제 ip 주소 중의 처음 것을 반환
    let getPhysicalIPAdress() =
        getPhysicalIPAdresses()
        |> List.tryExactlyOne
        |> Option.defaultValue("")
            