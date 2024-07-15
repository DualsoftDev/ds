namespace Engine.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open System.IO
open System
open PPTUtil
open Dual.Common.Core.FS
open Engine.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module PPTConnectionModule =

    ///전체 사용된 화살표 반환 (앞뒤연결 필수)
    let Connections (doc: PresentationDocument) =
        Office.SlidesSkipHide(doc)
        |> Seq.map (fun (slide, _) -> slide, slide.GetShapeTreeConnectionShapes())
        |> Seq.map (fun (slide, connects) ->
            slide,
            connects
            |> Seq.map (fun conn ->
                let Id = conn.Descendants<NonVisualDrawingProperties>().First().Id

                let startNode =
                    conn.Descendants<NonVisualConnectionShapeProperties>()
                        .First()
                        .Descendants<StartConnection>()
                        .FirstOrDefault()

                let endNode =
                    conn.Descendants<NonVisualConnectionShapeProperties>()
                        .First()
                        .Descendants<EndConnection>()
                        .FirstOrDefault()

                let connStartId = if (startNode = null) then 0u else startNode.Id.Value
                let connEndId = if (endNode = null) then 0u else endNode.Id.Value

                conn, Id, connStartId, connEndId))

    ///전체 사용된 도형간 그룹지정 정보
    let Groups (doc: PresentationDocument) =
        Office.SlidesSkipHide(doc)
        |> Seq.filter (fun (slide, _) -> slide.GetShapeTreeGroupShapes().Any())
        |> Seq.map (fun (slide, _) -> slide,  slide.GetShapeTreeGroupShapes()|> Seq.toList)

    let GetCausal (conn: ConnectionShape, iPage, startName, endName) =
        let shapeProperties = conn.Descendants<ShapeProperties>().FirstOrDefault()
        let outline = shapeProperties.Descendants<Outline>().FirstOrDefault()
        let tempHead, tempTail = outline.getConnectionHeadTail()

        let isChangeHead =
            (tempHead = LineEndValues.None |> not) && (tempTail = LineEndValues.None)

        let headShape = if (isChangeHead) then tempTail else tempHead
        let tailShape = if (isChangeHead) then tempHead else tempTail

        let existHead = headShape = LineEndValues.None |> not
        let existTail = tailShape = LineEndValues.None |> not

        let headArrow =
            headShape = LineEndValues.Triangle
            || headShape = LineEndValues.Arrow
            || headShape = LineEndValues.Stealth

        let tailArrow =
            tailShape = LineEndValues.Triangle
            || tailShape = LineEndValues.Arrow
            || tailShape = LineEndValues.Stealth

        let dashLine = Office.IsDashLine(conn)

        let single =
            outline.CompoundLineType = null
            || outline.CompoundLineType.Value = CompoundLineValues.Single

        let edgeProperties =
            conn.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault()

        let connStart = edgeProperties.Descendants<StartConnection>().FirstOrDefault()
        let connEnd = edgeProperties.Descendants<EndConnection>().FirstOrDefault()


        //연결오류 찾아서 예외처리
        if (connStart = null && connEnd = null) then
            conn.ErrorConnect(ErrID._4, startName, endName, iPage)

        if (connStart = null) then
            conn.ErrorConnect(ErrID._5, startName, endName, iPage)

        if (connEnd = null) then
            conn.ErrorConnect(ErrID._6, startName, endName, iPage)

        if (existHead && existTail) then
            if (not(headArrow || tailArrow)) then
                conn.ErrorConnect(ErrID._9, startName, endName, iPage)


        //인과 타입과 <START, END> 역전여부
        match existHead, existTail, dashLine with
        | true, true, true -> 
             if (not headArrow && tailArrow) then
                SelfReset, false
             else if (headArrow && not tailArrow) then
                SelfReset, true //반대로 뒤집기 필요
             else 
                Interlock, false
        | true, true, false ->
            if (not headArrow && tailArrow) then
                StartReset, false
            else
                StartReset, true //반대로 뒤집기 필요
        | _ ->
            match single, tailArrow, dashLine with
            | true, true, false -> StartEdge, isChangeHead
            //| false, true, false -> StartPush, isChangeHead //강연결 사용안함 24/03.08
            | false, true, false -> StartEdge, isChangeHead 
            | true, true, true -> ResetEdge, isChangeHead
            //| false, true, true -> ResetPush, isChangeHead   //강연결 사용안함 24/03.08
            | false, true, true -> ResetEdge, isChangeHead
            | _ -> conn.ErrorConnect(ErrID._3, startName, endName, iPage)

