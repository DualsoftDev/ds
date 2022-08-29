// https://ko.ojit.com/so/f%23/1666316 : XmlProvider 를 이용한 추가
// https://luketopia.net/2013/10/06/xml-transformations-with-fsharp/

#r "nuget: FSharp.Data"
#r "System.Xml.Linq.dll"
#r "System.Xml.dll"
#r "System.Xml.ReaderWriter.dll"

#I @"..\..\bin\netcoreapp3.1"
#r "Dual.Common.FS.dll"

open System.IO
open System.Diagnostics
open System.Linq
open System.Xml
open System.Xml.Linq
open System.Runtime.CompilerServices
open FSharp.Data
open Dual.Common




let xgi2 = @"C:\users\kwak\Documents\xgi2.xml"
let xdoc2 =
    @"C:\users\kwak\Documents\xgi2.xml"
    |> Dual.Common.DsXml.load

//let ncs = xdoc.SelectNodes("//Project//NetworkConfiguration")
let ncs = xdoc2.SelectNodes("Project/NetworkConfiguration/*")
ncs;;

let titleNode = xdoc2.SelectSingleNode("Project/NetworkConfiguration/NetworkLists/Network/@*");
titleNode.InnerText
let networks = xdoc2.SelectNodes("Project/NetworkConfiguration/NetworkLists/*");
networks.ToStrings()

let conf = xdoc2.SelectNodes("Project/Configurations/Configuration")
conf.ToStrings()

let confProperties = xdoc2.SelectNodes("Project/Configurations/Configuration/@*")
confProperties.ToStrings()

let bases = xdoc2.SelectNodes("Project/Configurations/Configuration/Parameters/Parameter/BaseInfo/*");
bases.ToStrings()


let programs = xdoc2.SelectSingleNode("//POU/Programs")
programs.OuterXml





// https://luketopia.net/2013/10/06/xml-transformations-with-fsharp/
let [<Literal>] orderSample = @"C:\users\kwak\Documents\input_sample.xml"

//let [<Literal>] orderSample =
//    """<?xml version="1.0" encoding="utf-8" ?>  
//    <Customers>  
//      <Customer name="ACME">
//        <Order Number="A012345">
//          <OrderLine Item="widget" Quantity="1"/>
//        </Order>
//        <Order Number="A012346">
//          <OrderLine Item="trinket" Quantity="2"/>
//        </Order>
//      </Customer>
//      <Customer name="Southwind">
//        <Order Number="A012347">
//          <OrderLine Item="skyhook" Quantity="3"/>
//          <OrderLine Item="gizmo" Quantity="4"/>
//        </Order>
//      </Customer>
//    </Customers>
//    """
type InputXml = XmlProvider<orderSample>  

//let input = InputXml.Load("input_sample.xml")
//let input = InputXml.GetSample()
let input = InputXml.Load(orderSample)
for customer in input.Customers do  
for order in customer.Orders do  
for line in order.OrderLines do  
    printfn "Customer: %s, Order: %s, Item: %s, Quantity: %d"
            customer.Name order.Number line.Item line.Quantity


