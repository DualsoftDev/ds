using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using System.Linq;
using System;
using DocumentFormat.OpenXml.Office.LongProperties;
using DocumentFormat.OpenXml;

namespace PresentationUtility
{
    public static class PresentationConnectionManager
    {
        public static ConnectionShape CreateConnectionShape(Slide slide, Shape startShape, Shape endShape)
        {

            uint id = GetNextShapeId(slide);
            string typeName = "Connector";

            // startShape 및 endShape로부터 Transform2D 정보 추출
            var startTransform2D = startShape.Descendants<ShapeProperties>()
                                              .FirstOrDefault()
                                              ?.Descendants<D.Transform2D>()
                                              .FirstOrDefault();
            var endTransform2D = endShape.Descendants<ShapeProperties>()
                                          .FirstOrDefault()
                                          ?.Descendants<D.Transform2D>()
                                          .FirstOrDefault();

            // Transform2D에서 Offset 정보 추출
            var startX = Convert.ToInt32(startTransform2D?.Offset?.X.Value ?? 0); // 기본값으로 0 설정
            var startY = Convert.ToInt32(startTransform2D?.Offset?.Y.Value ?? 0); // 기본값으로 0 설정
            var endX = Convert.ToInt32(endTransform2D?.Offset?.X.Value ?? 0); // 기본값으로 0 설정
            var endY = Convert.ToInt32(endTransform2D?.Offset?.Y.Value ?? 0); // 기본값으로 0 설정

            // 시작점과 종료점을 Tuple<int, int>로 설정
            Tuple<int, int> startPoint = Tuple.Create(startX, startY);
            Tuple<int, int> endPoint = Tuple.Create(endX, endY);


            var connectionShape = new ConnectionShape()
            {
                NonVisualConnectionShapeProperties = CreateNonVisualConnectionShapeProperties(id, typeName, startShape, endShape),
                ShapeProperties = CreateShapeProperties(startPoint, endPoint),
                ShapeStyle = CreateShapeStyle()
            };

            return connectionShape;
        }

   
        private static bool DetermineVerticalFlip(Tuple<int, int> startPoint, Tuple<int, int> endPoint)
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


        private static NonVisualConnectionShapeProperties CreateNonVisualConnectionShapeProperties(uint id, string typeName, Shape startShape, Shape endShape)
        {
            return new NonVisualConnectionShapeProperties
            {
                NonVisualDrawingProperties = new NonVisualDrawingProperties { Id = id, Name = $"{typeName} {id}" },
                NonVisualConnectorShapeDrawingProperties = new NonVisualConnectorShapeDrawingProperties
                {
                    StartConnection = new D.StartConnection { Id = startShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = 3 },
                    EndConnection = new D.EndConnection { Id = endShape.NonVisualShapeProperties.NonVisualDrawingProperties.Id, Index = 1 }
                },
                ApplicationNonVisualDrawingProperties = new ApplicationNonVisualDrawingProperties()
            };
        }

        private static ShapeProperties CreateShapeProperties(Tuple<int, int> startPoint, Tuple<int, int> endPoint)
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
