



// https://ko.ojit.com/so/f%23/1666316 : XmlProvider 를 이용한 추가
// https://luketopia.net/2013/10/06/xml-transformations-with-fsharp/

#r "nuget: FSharp.Data"
#r "System.Xml.Linq.dll"
#r "System.Xml.ReaderWriter.dll"


open FSharp.Data
open System.IO
open System.Diagnostics
open System.Linq
open System.Xml
open System.Xml.Linq
open System.Runtime.CompilerServices

let tracef  fmt = Printf.kprintf Trace.Write fmt
let tracefn fmt = Printf.kprintf Trace.WriteLine fmt

let [<Literal>] template = @"C:\users\kwak\Documents\xgi2.xml"

type Xgi = XmlProvider<template, Global=true>

let sample = Xgi.GetSample()
//let sample =
//    "C:\\users\\kwak\\Documents\\xgi.xml"
//    |> File.ReadAllText
//    |> Xgi.Parse
let sample = Xgi.Load "C:\\users\\kwak\\Documents\\xgi.xml"


tracefn "%s" sample
sample
sample.Root
sample.Attribute
sample.Version
sample.Guid
sample.NetworkConfiguration
sample.NetworkConfiguration.NetworkList
master.NetworkConfiguration.NetworkLists

master.NetworkConfiguration.XElement


sample.NetworkConfiguration.Network.Type
sample.NetworkConfiguration.Network
sample.Configuration.ToString()
doc.NetworkConfiguration.GetType()




//let [<Literal>] person =
//    """<?xml version="1.0" encoding="utf-8"?>
//    <Persons>
//      <Person>
//        <Name>Person 1</Name>
//        <Age>30</Age>
//      </Person>
//      <Person>
//        <Name>Person 2</Name>
//        <Age>32</Age>
//      </Person>
//    </Persons>"""
let [<Literal>] person =
    """<?xml version="1.0" encoding="utf-8"?>
    <Persons>
      <Person>
        <Name>Person 1</Name>
        <Age>30</Age>
      </Person>
    </Persons>"""

type PersonXmlProvider = XmlProvider<person>

let master = PersonXmlProvider.GetSample()
master.XElement
master.Persons
master.Persons.[0]

let person = PersonXmlProvider.Person("Person 3", 33)
master.XElement.Add(person.XElement)

personsXml.XElement.Add(new PersonXmlProvider.Person("Person 3", 33))
personsXml.XElement.Save("Persons.xml")



let [<Literal>] xgi =
    """<?xml version="1.0" encoding="UTF-8"?>
    <Project>
        <NetworkConfiguration>
		    <NetworkLists>
			    <Network Type="NETWORK ITEM:UNKNOWN" Name="1기본 네트워크" NetworkType=""></Network>
		    </NetworkLists>
	    </NetworkConfiguration>
	    <SystemVariable HMIGUID=""></SystemVariable>
    </Project>
"""
type XgiXmlProvider = XmlProvider<xgi>

let master = XgiXmlProvider.GetSample()
master.NetworkConfiguration.Network Lists





// XPATH: https://funylife.tistory.com/entry/2-XPath%EC%9D%98-%EA%B8%B0%EB%B3%B8%EB%AC%B8%EB%B2%95%EC%9D%84-%EC%95%8C%EC%95%84%EB%B3%B4%EC%9E%90

[<Extension>]
type XmlExt =
    // https://stackoverflow.com/questions/21871908/converting-xmlnodelist-to-liststring
    [<Extension>]
    static member ToStrings(xmlNodeList:XmlNodeList) =
        System.Linq.Enumerable.Cast<XmlNode>(xmlNodeList)
        //|> Seq.map (fun node -> node.InnerText)
        |> List.ofSeq
        |> List.map (fun node -> node.OuterXml)


