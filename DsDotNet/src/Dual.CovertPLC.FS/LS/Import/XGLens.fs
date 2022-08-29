namespace Dual.ConvertPLC.FS.LsXGI

open FSharpPlus
open Dual.Common
open System
open AddressConvert
open System.Collections.Generic
open Config.POU.Program.LDRoutine

[<AutoOpen>]
type AddressType = 
    | input = 0
    | output = 1
    | memory = 2
    | physical = 3
/// XG5000 을 프로젝트 파일을 Xml로 저장한 후 추출한 XGLensTag 클래스
type XGLensTag(name, symbolInfo:SymbolInfo) = 
    let mutable rungLens : ResizeArray<string> = ResizeArray() //"%s‡%d‡%d" tagMarking posMarking e.ElementType 형상저장
    let mutable contactIDs  : ResizeArray<int> = ResizeArray() 
    let mutable coilIDs  : ResizeArray<int> = ResizeArray() 
    let mutable fbIDs  : ResizeArray<int> = ResizeArray() 
    let mutable subConditions  : ResizeArray<XGLensTag> = ResizeArray() 
    let mutable isSetResetDummy = false
    let mutable isDeviceFlag = false
    let mutable id = -1
    let symbolInfo = symbolInfo

    member val ID = id with get, set
    member val Name = name with get, set
    member val AddressType = AddressType.memory with get, set

    member x.SubConditions = subConditions
    member x.Address = symbolInfo.Address
    member x.Device = symbolInfo.Device
    member x.DeviceSize = symbolInfo.Type
    member x.ContactCount with get() =  contactIDs.Count
    member x.CoilCount with get() =  coilIDs.Count
    member x.FBxgkCount with get() =  fbIDs.Count
    member x.Comment = symbolInfo.Comment
    member x.GetSymbolInfo()   = symbolInfo
  
    member x.GetLensKeys() = String.Join(",", rungLens)
    member x.RungLens           with get() = rungLens           and set(v) = rungLens <- v
    member x.IsSetResetDummy    with get() = isSetResetDummy    and set(v) = isSetResetDummy <- v
    member x.IsDeviceFlag       with get() = isDeviceFlag       and set(v) = isDeviceFlag <- v
    member x.CoilIDs            with get() = coilIDs            and set(v) = coilIDs <- v
    member x.ContactIDs         with get() = contactIDs         and set(v) = contactIDs <- v
    member x.FBxgk              with get() = fbIDs              and set(v) = fbIDs <- v



module XGLens =
    
    ///xml Tag로 부터 LensTag를 생성한다.
    let getLens(xmlXG5000:XmlXG5000) =
        let cpu = xmlXG5000.Cpu

        let globalVars = xmlXG5000.GlobalVars |> Seq.map(fun f-> XGLensTag(f.Name, f))
        let directVars = xmlXG5000.DirectVars |> Seq.map(fun f-> XGLensTag(f.Name, f))
        let systemTags = xmlXG5000.SystemVars
                        |> Seq.filter(fun f -> f.Device ="F")
                        |> Seq.map(fun f->
                            let sysLens = XGLensTag(f.Name, f)
                            sysLens.IsSetResetDummy <- true
                            sysLens)

        let dicExistVar = 
            globalVars @@ directVars @@ systemTags 
            |> Seq.map(fun v -> v.Address, v)
            |> dict

        let dicAblelogicTags = Dictionary<string, XGLensTag>()
        let getlogicTag tagAddress tagFB= 
            tagAddress @@ tagFB
            |> Seq.filter(fun address-> not (dicExistVar.ContainsKey(address)))
            |> Seq.map(fun address -> FileRead.getSymbolFromAddress(address, "", "", cpu))
            |> Seq.filter(fun v ->  v.IsSome)
            |> Seq.map(fun v -> 
                if(not (dicAblelogicTags.ContainsKey(v.Value.Address)))
                then 
                    let newTag = XGLensTag(v.Value.Name, v.Value)
                    dicAblelogicTags.Add(v.Value.Address, newTag) 
                    newTag
                else 
                    dicAblelogicTags.[v.Value.Address]
                )


        let logicVarTags = 
            xmlXG5000.Rungs
            |> Seq.collect(fun rung-> 
                let tagAddress = rung.Elements |> Seq.map(fun ele -> ele.AddressSet |> fun (address, addressType)-> address)
                let tagFBXGK = rung.Elements |> Seq.collect(fun ele -> ele.FBXGK |> Seq.map(fun (address)-> address))
                let tagFBXGI = rung.Elements |> Seq.collect(fun ele -> ele.AddressFBXGI |> Seq.map(fun (address, addressType)-> address))
                getlogicTag tagAddress (tagFBXGK @@ tagFBXGI))

        let totalVar = 
            globalVars @@ directVars @@ systemTags @@ logicVarTags 
            |> Seq.map(fun v -> v.Address, v)
            |> dict


        let xgkCMD = 
            xmlXG5000.XgkCMD 
            |> Seq.filter(fun f-> f.bySize <> 0)
            |> Seq.map(fun f-> f.Command, ([|f.Command|] @@ f.strStatement.Split(' ')) |> Seq.toList)
            |> dict


        let dummySetReset = ResizeArray<XGLensTag>()
        let SettingLogicTAG (elementType, coordinate, address, rungID, rungLens) =
            if(totalVar.ContainsKey(address))
            then    
                let tag = totalVar.[address]
                let isContact =  elementType <= 13
                if (isContact)
                then
                    tag.ContactIDs.Add(rungID);
                else
                    if (elementType = 16 || elementType = 17)
                    then
                        let newTag =  totalVar.[address]
                        let newName =  if(elementType = 16) then "[S] "+ newTag.Name else "[R] "+ newTag.Name
                        let newLensTag = 
                            if (not (totalVar.ContainsKey(newName)))
                            then 
                                XGLensTag(newName, newTag.GetSymbolInfo())
                            else
                                totalVar.[newName]

                        newLensTag.IsSetResetDummy <- true
                        newLensTag.RungLens.AddRange(rungLens)
                        newLensTag.CoilIDs.Add(rungID)
                        dummySetReset.Add(newLensTag)
                    else
                        tag.RungLens.AddRange(rungLens)
                        tag.CoilIDs.Add(rungID);
            else   ()
            
           
        let SettingLogicFBXGI (elementType, coordinate, address, rungID, rungLens, fbPosition) =
            if(totalVar.ContainsKey(address) && address <> "")
            then    
                let tag = totalVar.[address]
                let isContact = 
                    if(elementType = (int)ElementType.VariableMode)
                    then (fbPosition >= (coordinate - 1) % 1024 / 3)
                    else failwithlogf "elementType Not XGI FB '%s'" (elementType.ToString())

                if (isContact)
                then
                    tag.ContactIDs.Add(rungID);
                    
                else
                    tag.RungLens.AddRange(rungLens)
                    tag.CoilIDs.Add(rungID);
            else   ()

        let SettingLogicFBXGK_Detail (elementType, coordinate, fbXGKs, rungID, rungLens, cmd) =
            let mutable indexTag = -1 // ex) "BMOV,F0050,M0010,1" 초기 CMD는 타입 분석에서 생략
            fbXGKs
            |> Seq.iter(fun (param) ->
                indexTag <-indexTag+1
                if(totalVar.ContainsKey(param))
                then
                    let tag = totalVar.[param]
                    let isContact = 
                        if(elementType = (int)ElementType.FBMode)  // 비교 연산 경우 AND<는 <로 저장 되어있음 색인에러
                        then xgkCMD.[cmd].[indexTag].ToUpper().StartsWith("S") //Source
                        else failwithlogf "elementType Not XGK FB '%s'" (elementType.ToString())

                    if (isContact)
                    then
                        tag.ContactIDs.Add(rungID);
                    else
                        tag.RungLens.AddRange(rungLens)
                        tag.CoilIDs.Add(rungID);
                else   ()
            )

        let SettingLogicFBXGK (elementType, coordinate, fbXGKs, rungID, rungLens, cmd) =
            fbXGKs
            |> Seq.iter(fun (param) ->
                if(totalVar.ContainsKey(param))
                then
                    let tag = totalVar.[param]
                    tag.FBxgk.Add(rungID);
                else   ()
            )

        xmlXG5000.Rungs
        |> Seq.iter(fun rung->
            let rungID = rung.RungID
            let rungLens = rung.RungLens |> ResizeArray
            let mutable fbPosition = 0
            rung.Elements
            |> Seq.iter(fun ele ->
                let tagName = ele.Tag
                let elementType = ele.ElementType
                let coordinate = ele.Coordinate
                let address, addressType = ele.AddressSet
                fbPosition <-
                    if (elementType = (int)ElementType.VertFuncMode || elementType = (int)ElementType.VertFBMode || elementType = (int)ElementType.FBMode)
                    then (coordinate - 1) % 1024 / 3 else fbPosition

                ele.AddressFBXGI 
                |> Seq.iter(fun (addressFB, addressTypeFB) ->  SettingLogicFBXGI(elementType, coordinate, addressFB, rungID, rungLens, fbPosition))

                if(ele.FBXGK.length() > 0) 
                then SettingLogicFBXGK(elementType, coordinate, ele.FBXGK, rungID, rungLens, tagName) 
                else ()

                if (elementType <> (int)ElementType.VariableMode) 
                then SettingLogicTAG(elementType, coordinate, address , rungID, rungLens)
                else ()
                ) 
                )
                
        let mutable tagCnt = 0
        totalVar.Values @@ dummySetReset
            |> Seq.iter(fun v-> 
                        if(v.Device = "I")
                        then  v.AddressType <- AddressType.input
                        else if(v.Device = "Q")
                        then  v.AddressType <- AddressType.output
                        else if(v.Device = "P")
                        then  v.AddressType <- AddressType.physical
                        else  v.AddressType <- AddressType.memory

                        v.ID <- tagCnt
                        tagCnt <- tagCnt + 1
                        )


        let listTags = 
            totalVar.Values
            |> Seq.filter(fun v-> v.Address <> "")
            |> Seq.toList
            
        listTags @@ dummySetReset
        
        
           
