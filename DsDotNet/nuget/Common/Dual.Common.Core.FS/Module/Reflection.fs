namespace Dual.Common.Core.FS

open System
open System.Net
open System.Net.Sockets
open System.Runtime.CompilerServices
open System.Net.NetworkInformation
open System.Reflection
open System.Xml.Schema



[<AutoOpen>]
module Reflection =
    type Assembly with
        member x.FindImplementingTypes (interfaceType: Type) =
            x.GetTypes()
            |> Array.filter (fun t -> interfaceType.IsAssignableFrom(t) && not t.IsAbstract && t.IsClass)
        static member XXXXmlSchemaXPath() = ()
    
    
    //let findImplementingTypes (assembly: Assembly) (interfaceType: Type) =
    //    assembly.GetTypes()
    //    |> Array.filter (fun t -> interfaceType.IsAssignableFrom(t) && not t.IsAbstract && t.IsClass)


    //let assembly = Assembly.LoadFrom(assemblyPath)

    //// 찾고자 하는 인터페이스의 타입을 지정합니다.
    //let interfaceType = typeof<IXXX> // 'IXXX'를 귀하가 찾고자 하는 인터페이스의 실제 이름으로 바꾸세요.

    //// 인터페이스를 구현하는 클래스 검색
    //let implementingTypes = findImplementingTypes assembly interfaceType

    //// 결과 출력
    //implementingTypes |> Array.iter (fun t -> printfn "%s" t.FullName)

