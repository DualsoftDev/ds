using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Office.LongProperties;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml;

using Drawing = DocumentFormat.OpenXml.Drawing;

using System.Linq;
using System;

using static Engine.Core.DsText;
using static Engine.Core.CoreModule;

namespace Engine.Export.Office
{
    public static class ConnectionManager
    {
        public static ConnectionShape CreateConnectionShape(Slide slide, Shape startShape, Shape endShape, ModelingEdgeType edgeType, bool bRev)
        {
            uint id = GetNextShapeId(slide);
            string typeName = "Connector";
            bool isStartRect = IsRectShape(startShape);
            var isLeftToRight = get2DPoint(startShape).Offset.Y == get2DPoint(endShape).Offset.Y;

            Tuple<long, long> startPoint, endPoint;
            if (isLeftToRight)
                setPointLeftToRight(startShape, endShape, out startPoint, out endPoint);
            else
                setPointTopToBottom(startShape, endShape, out startPoint, out endPoint);

            var connectionShape = new ConnectionShape()
            {
                NonVisualConnectionShapeProperties = CreateNonVisualConnectionShapeProperties(id, typeName, startShape, endShape, isStartRect, isLeftToRight),
                ShapeProperties = CreateShapeProperties(startPoint, endPoint, edgeType, bRev),
                ShapeStyle = CreateShapeStyle()
            };

            slide.CommonSlideData.ShapeTree.AppendChild(connectionShape);
            return connectionShape;
        }

        private static bool IsRectShape(Shape startShape)
        {
            return startShape
                                .Descendants<ShapeProperties>()
                                .First()
                                .Descendants<Drawing.PresetGeometry>()
                                .FirstOrDefault().Preset.Value == Drawing.ShapeTypeValues.Rectangle;
        }
        private static void setPointLeftToRight(Shape startShape, Shape endShape, out Tuple<long, long> startPoint, out Tuple<long, long> endPoint)
        {
            Drawing.Transform2D startTransform2D = get2DPoint(startShape);
            Drawing.Transform2D endTransform2D = get2DPoint(endShape);

            var startX = startTransform2D.Offset.X.Value + startTransform2D.Extents.Cx;
            var startY = startTransform2D.Offset.Y.Value + startTransform2D.Extents.Cy / 2;
            var endX = endTransform2D.Offset.X.Value;
            var endY = endTransform2D.Offset.Y.Value + endTransform2D.Extents.Cy / 2;

            // 시작점과 종료점을 Tuple<int, int>로 설정
            startPoint = Tuple.Create(startX, startY);
            endPoint = Tuple.Create(endX, endY);
        }
        private static void setPointTopToBottom(Shape startShape, Shape endShape, out Tuple<long, long> startPoint, out Tuple<long, long> endPoint)
        {
            Drawing.Transform2D startTransform2D = get2DPoint(startShape);
            Drawing.Transform2D endTransform2D = get2DPoint(endShape);

            var startX = startTransform2D.Offset.X.Value + startTransform2D.Extents.Cx / 2;
            var startY = startTransform2D.Offset.Y.Value + startTransform2D.Extents.Cy;
            var endX = endTransform2D.Offset.X.Value + startTransform2D.Extents.Cx / 2;
            var endY = endTransform2D.Offset.Y.Value;

            // 시작점과 종료점을 Tuple<int, int>로 설정
            startPoint = Tuple.Create(startX, startY);
            endPoint = Tuple.Create(endX, endY);
        }

        private static Drawing.Transform2D get2DPoint(Shape startShape)
        {
            // startShape 및 endShape로부터 Transform2D 정보 추출
            return startShape.Descendants<ShapeProperties>()
                                              .FirstOrDefault()
                                              ?.Descendants<Drawing.Transform2D>()
                                              .FirstOrDefault();
        }
        private static bool DetermineVerticalFlip(Tuple<long, long> startPoint, Tuple<long, long> endPoint)
        {
            // XOR to determine if the line needs to be vertically flipped for aesthetic reasons.
            return (endPoint.Item1 > startPoint.Item1) ^ (endPoint.Item2 > startPoint.Item2);
        }

        private static ShapeStyle CreateShapeStyle()
        {
            ShapeStyle shapeStyle1 = new ShapeStyle();
            Drawing.LineReference lineReference1 = new Drawing.LineReference() { Index = (UInt32Value)2U };

            Drawing.SchemeColor schemeColor1 = new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Accent1 };
            Drawing.Shade shade1 = new Drawing.Shade() { Val = 50000 };

            schemeColor1.Append(shade1);

            lineReference1.Append(schemeColor1);

            Drawing.FillReference fillReference1 = new Drawing.FillReference() { Index = (UInt32Value)1U };
            Drawing.SchemeColor schemeColor2 = new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Accent1 };

            fillReference1.Append(schemeColor2);

            Drawing.EffectReference effectReference1 = new Drawing.EffectReference() { Index = (UInt32Value)0U };
            Drawing.SchemeColor schemeColor3 = new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Accent1 };

            effectReference1.Append(schemeColor3);

            Drawing.FontReference fontReference1 = new Drawing.FontReference() { Index = Drawing.FontCollectionIndexValues.Minor };
            Drawing.SchemeColor schemeColor4 = new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Light1 };

            fontReference1.Append(schemeColor4);

            shapeStyle1.Append(lineReference1);
            shapeStyle1.Append(fillReference1);
            shapeStyle1.Append(effectReference1);
            shapeStyle1.Append(fontReference1);
            return shapeStyle1;
        }


        private static NonVisualConnectionShapeProperties CreateNonVisualConnectionShapeProperties(uint id, string typeName, Shape startShape, Shape endShape, bool isRect, bool isLeftToRight)
        {

            var startConnectionPoint = isRect
                                      ? isLeftToRight ? 3u : 2u
                                      : isLeftToRight ? 6u : 4u;


            var endConnectionPoint = isRect
                                      ? isLeftToRight ? 1u : 0u
                                      : isLeftToRight ? 2u : 0u;


            var ret = new NonVisualConnectionShapeProperties
            {
                NonVisualDrawingProperties = new NonVisualDrawingProperties { Id = id, Name = $"{typeName} {id}" },
                NonVisualConnectorShapeDrawingProperties = new NonVisualConnectorShapeDrawingProperties
                {

                    StartConnection = new Drawing.StartConnection { Id = startShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = startConnectionPoint },
                    EndConnection = new Drawing.EndConnection { Id = endShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = endConnectionPoint }
                },
                ApplicationNonVisualDrawingProperties = new ApplicationNonVisualDrawingProperties()
            };
            return ret;
        }

        private static ShapeProperties CreateShapeProperties(Tuple<long, long> startPoint, Tuple<long, long> endPoint, ModelingEdgeType edgeType, bool bRev)
        {
            var shapeProperties = new ShapeProperties();
            bool verticalFlip = DetermineVerticalFlip(startPoint, endPoint);

            // Transform 설정
            var transform2D = new Drawing.Transform2D
            {
                VerticalFlip = verticalFlip,
                Offset = new Drawing.Offset { X = Math.Min(startPoint.Item1, endPoint.Item1), Y = Math.Min(startPoint.Item2, endPoint.Item2) },
                Extents = new Drawing.Extents { Cx = Math.Abs(endPoint.Item1 - startPoint.Item1), Cy = Math.Abs(endPoint.Item2 - startPoint.Item2) }
            };
            shapeProperties.Append(transform2D);
            // PresetGeometry 설정: 연결선 형태 정의
            var presetGeometry = new Drawing.PresetGeometry { Preset = Drawing.ShapeTypeValues.BentConnector2 };
            presetGeometry.Append(new Drawing.AdjustValueList());
            shapeProperties.Append(presetGeometry);

            // Outline 설정: 선 두께와 색상 지정
            var outline = new Drawing.Outline
            {
                Width = 12700 * 3, // 선 두께를 3pt로 설정
                CapType = Drawing.LineCapValues.Flat,
            };
            var solidFill = new Drawing.SolidFill(new Drawing.RgbColorModelHex { Val = "909090" }); // 선 색상을 검정으로 설정
            outline.Append(solidFill);
            shapeProperties.Append(outline);



            var head = Drawing.LineEndValues.None;
            var tail = Drawing.LineEndValues.Arrow;
            var lineType = Drawing.PresetLineDashValues.Solid;

            if (edgeType.IsStartEdge || edgeType.IsStartPush) { };
            if (edgeType.IsResetEdge || edgeType.IsResetPush) { lineType = Drawing.PresetLineDashValues.Dash; }
            if (edgeType.IsInterlockWeak || edgeType.IsInterlockStrong) { lineType = Drawing.PresetLineDashValues.Dash; head = Drawing.LineEndValues.Arrow; }
            if (edgeType.IsStartReset) { head = Drawing.LineEndValues.Diamond; }

            if (bRev)
            {
                var tailTemp = tail;
                tail = head;
                head = tailTemp;
            }

            outline.Append(new Drawing.PresetDash() { Val = lineType });

           
            var headEnd= new Drawing.HeadEnd { Type = head, Width = Drawing.LineEndWidthValues.Medium, Length = Drawing.LineEndLengthValues.Medium };
            var tailEnd = new Drawing.TailEnd { Type = tail, Width = Drawing.LineEndWidthValues.Medium, Length = Drawing.LineEndLengthValues.Medium };

            outline.AddChild(headEnd);
            outline.AddChild(tailEnd);

            return shapeProperties;
        }

        private static uint GetNextShapeId(Slide slide)
        {
            return slide.CommonSlideData.ShapeTree.Descendants<NonVisualDrawingProperties>()
                   .Select(e => e.Id.Value)
                   .DefaultIfEmpty(0U)
                   .Max() + 1;
        }
    }
}
