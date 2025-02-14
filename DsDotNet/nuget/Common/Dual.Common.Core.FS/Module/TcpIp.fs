namespace Dual.Common.Core.FS

open System
open System.Net
open System.Net.Sockets
open System.Runtime.CompilerServices
open System.Net.NetworkInformation

[<AutoOpen>]
module IPAddressModule =
    /// local system 의 IP addresses(복수개) 를 반환한다.
    let getIpAddresses() =
        Dns.GetHostAddresses(Dns.GetHostName())
            |> Seq.map _.ToString()
            |> Seq.filter _.Contains(".")

    let getIpAddress() : string =
        getIpAddresses() |> Seq.head


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


    let getIpAddressFromHostname(hostname:string) =
        let ipaddress =
            Dns.GetHostAddresses(hostname)
            |> Seq.find(fun ip -> ip.AddressFamily = Sockets.AddressFamily.InterNetwork)
        ipaddress.ToString()

    let ipAddressEqual (a:string) (b:string) =
        try
            match a, b with
            | null, _ | _, null | "", _ | _, "" -> false
            | a, b when a = b -> true
            | _ ->
                match IPAddress.TryParse(a), IPAddress.TryParse(b) with
                | (true, _), (true, _) -> failwithlog "Check it again!!!"
                | (true, _), (false, _) ->
                    a = getIpAddressFromHostname(b)
                | (false, _), (true, _) ->
                    b = getIpAddressFromHostname(a)
                | (false, _), (false, _) ->
                    getIpAddressFromHostname(a) = getIpAddressFromHostname(b)
        with exn ->
            false


[<AutoOpen>]
[<Extension>]
type SocketExt =
    [<Extension>]
    static member AsyncAccept(socket:Socket) =
        Async.FromBeginEnd(socket.BeginAccept, socket.EndAccept)


    [<Extension>]
    static member AsyncReceive(socket:Socket, buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginReceive (b, o, c, cb, s) =
            socket.BeginReceive(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginReceive, socket.EndReceive)

    /// 비동기로 주어진 count 만큼 읽어들임
    [<Extension>]
    static member AsyncRead(socket:Socket, count) =
        let buffer = Array.zeroCreate<Byte> (count)

        let rec loop offset remaining =
            async {
                let! read = SocketExt.AsyncReceive(socket, buffer, offset, remaining)
                if remaining - read = 0 then
                    return buffer
                else
                    return! loop (offset + read) (remaining - read)
            }

        loop 0 count

    [<Extension>]
    static member AsyncSend(socket:Socket, buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginSend (b, o, c, cb, s) =
            socket.BeginSend(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginSend, socket.EndSend)



    /// Socket 이 server 에 의해서 끊겼는지를 검사
    // https://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
    [<Extension>]
    static member IsConnected(socket:Socket) =
        try
            not (isNull socket) && not (socket.Poll(1, SelectMode.SelectRead) && socket.Available = 0);
        with exn ->
            false

