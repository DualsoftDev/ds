using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using System.Linq;
using System;
using DocumentFormat.OpenXml.Office.LongProperties;
using DocumentFormat.OpenXml;

namespace Engine.Export.Office
{
    public static class ConnectionManager
    {
        public static ConnectionShape CreateConnectionShape(Slide slide, Shape startShape, Shape endShape)
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
                ShapeProperties = CreateShapeProperties(startPoint, endPoint),
                ShapeStyle = CreateShapeStyle()
            };

            return connectionShape;
        }

        private static bool IsRectShape(Shape startShape)
        {
            return startShape
                                .Descendants<ShapeProperties>()
                                .First()
                                .Descendants<D.PresetGeometry>()
                                .FirstOrDefault().Preset.Value == D.ShapeTypeValues.Rectangle;
        }
        private static void setPointLeftToRight(Shape startShape, Shape endShape, out Tuple<long, long> startPoint, out Tuple<long, long> endPoint)
        {
            D.Transform2D startTransform2D = get2DPoint(startShape);
            D.Transform2D endTransform2D = get2DPoint(endShape);

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
            D.Transform2D startTransform2D = get2DPoint(startShape);
            D.Transform2D endTransform2D = get2DPoint(endShape);

            var startX = startTransform2D.Offset.X.Value + startTransform2D.Extents.Cx / 2;
            var startY = startTransform2D.Offset.Y.Value + startTransform2D.Extents.Cy;
            var endX = endTransform2D.Offset.X.Value + startTransform2D.Extents.Cx / 2;
            var endY = endTransform2D.Offset.Y.Value;

            // 시작점과 종료점을 Tuple<int, int>로 설정
            startPoint = Tuple.Create(startX, startY);
            endPoint = Tuple.Create(endX, endY);
        }

        private static D.Transform2D get2DPoint(Shape startShape)
        {
            // startShape 및 endShape로부터 Transform2D 정보 추출
            return startShape.Descendants<ShapeProperties>()
                                              .FirstOrDefault()
                                              ?.Descendants<D.Transform2D>()
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
            D.LineReference lineReference1 = new D.LineReference() { Index = (UInt32Value)2U };

            D.SchemeColor schemeColor1 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };
            D.Shade shade1 = new D.Shade() { Val = 50000 };

            schemeColor1.Append(shade1);

            lineReference1.Append(schemeColor1);

            D.FillReference fillReference1 = new D.FillReference() { Index = (UInt32Value)1U };
            D.SchemeColor schemeColor2 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };

            fillReference1.Append(schemeColor2);

            D.EffectReference effectReference1 = new D.EffectReference() { Index = (UInt32Value)0U };
            D.SchemeColor schemeColor3 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };

            effectReference1.Append(schemeColor3);

            D.FontReference fontReference1 = new D.FontReference() { Index = D.FontCollectionIndexValues.Minor };
            D.SchemeColor schemeColor4 = new D.SchemeColor() { Val = D.SchemeColorValues.Light1 };

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

                    StartConnection = new D.StartConnection { Id = startShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = startConnectionPoint },
                    EndConnection = new D.EndConnection { Id = endShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = endConnectionPoint }
                },
                ApplicationNonVisualDrawingProperties = new ApplicationNonVisualDrawingProperties()
            };
            return ret;
        }

        private static ShapeProperties CreateShapeProperties(Tuple<long, long> startPoint, Tuple<long, long> endPoint)
        {
            var shapeProperties = new ShapeProperties();
            bool verticalFlip = DetermineVerticalFlip(startPoint, endPoint);

            // Transform 설정
            var transform2D = new D.Transform2D
            {
                VerticalFlip = verticalFlip,
                Offset = new D.Offset { X = Math.Min(startPoint.Item1, endPoint.Item1), Y = Math.Min(startPoint.Item2, endPoint.Item2) },
                Extents = new D.Extents { Cx = Math.Abs(endPoint.Item1 - startPoint.Item1), Cy = Math.Abs(endPoint.Item2 - startPoint.Item2) }
            };
            shapeProperties.Append(transform2D);
            // PresetGeometry 설정: 연결선 형태 정의
            var presetGeometry = new D.PresetGeometry { Preset = D.ShapeTypeValues.BentConnector2 };
            presetGeometry.Append(new D.AdjustValueList());
            shapeProperties.Append(presetGeometry);

            // Outline 설정: 선 두께와 색상 지정
            var outline = new D.Outline
            {
                Width = 12700 * 3, // 선 두께를 3pt로 설정
                CapType = D.LineCapValues.Flat,
            };
            var solidFill = new D.SolidFill(new D.RgbColorModelHex { Val = "909090" }); // 선 색상을 검정으로 설정
            outline.Append(solidFill);
            shapeProperties.Append(outline);

            var tailEnd = new D.TailEnd
            {
                Type = D.LineEndValues.Arrow,
                Width = D.LineEndWidthValues.Medium,
                Length = D.LineEndLengthValues.Medium
            }; 
            var headEnd = new D.HeadEnd
            {
                Type = D.LineEndValues.Diamond,
                Width = D.LineEndWidthValues.Medium,
                Length = D.LineEndLengthValues.Medium
            };

            outline.AddChild(tailEnd);
            outline.AddChild(headEnd);

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
