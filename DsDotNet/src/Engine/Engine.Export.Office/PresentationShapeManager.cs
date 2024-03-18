using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace PresentationUtility
{
    public class ShapeManager
    {

        public static void AddShape(Slide slide, int count)
        {
            // SlidePart를 사용하여 ShapeTree에 접근
            var shapeTree = slide.CommonSlideData.ShapeTree;

            Shape lastShape = null; 
            Shape secondLastShape = null;

            for (int i = 0; i < count; i++)
            {
                uint drawingObjectId = GetNextShapeId(slide);
                long offsetX = 2000000 * i; // X 좌표 (2인치 = 914400 EMUs)
                long offsetY = 2000000; // Y 좌표 고정
                var shape = CreateNormalShape($"Shape{i + 1}", drawingObjectId, Drawing.ShapeTypeValues.Rectangle, offsetX, offsetY);
                shapeTree.AppendChild(shape);

                // 마지막 두 도형 ID 저장
                if (i == count - 2) secondLastShape = shape;
                if (i == count - 1) lastShape = shape;
            }

            // 도형이 두 개 이상 있을 때만 연결선 추가
            if (count > 1)
            {
                var conn = PresentationConnectionManager.CreateConnectionShape(slide, secondLastShape, lastShape);
                shapeTree.AppendChild(conn);
            }
        }

        // 다음 사용 가능한 Shape ID를 가져오는 메서드
        private static uint GetNextShapeId(Slide slide)
        {
            var ids = slide.Descendants<NonVisualDrawingProperties>()
                       .Select(e => e.Id.Value)
                       .DefaultIfEmpty(1U)
                       .Max() + 1;
            return ids;
        }


        public static Shape CreateTitleShape(string shapeName)
        {
            return new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = 1024, Name = "Title 1" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Title })),
                new ShapeProperties(),
                new TextBody(
                    new Drawing.BodyProperties(),
                    new Drawing.ListStyle(),
                    new Drawing.Paragraph(new Drawing.Run(new Drawing.Text(shapeName)))));
        }
        public static Shape CreateNormalShape(string shapeName, uint drawingObjectId, Drawing.ShapeTypeValues shapeTypeValue, long offsetX, long offsetY)
        {
            // 도형의 크기 설정
            long width = Convert.ToInt64(3.5 * 360000); // 폭 3.5cm
            long height = Convert.ToInt64(1.2 * 360000); // 높이 1.2cm

            var shape = new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = drawingObjectId, Name = $"Shape {drawingObjectId}" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),

                new ShapeProperties(
                    new Drawing.Transform2D(
                        new Drawing.Offset() { X = offsetX, Y = offsetY },
                        new Drawing.Extents() { Cx = width, Cy = height }),
                    new Drawing.PresetGeometry(new Drawing.AdjustValueList()) { Preset = shapeTypeValue }, // 직사각형 형태 지정
                    new Drawing.SolidFill(new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Accent1 }),
                    new Drawing.Outline(new Drawing.SolidFill(new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Dark1 }))
                    {
                        Width = 12700 // 윤곽선 두께 (단위: EMU, 예: 12700 EMU ≈ 1pt)
                    }
                ),

                new TextBody(
                    new Drawing.BodyProperties(),
                    new Drawing.ListStyle(),
                    new Drawing.Paragraph(
                        new Drawing.ParagraphProperties() { Alignment = Drawing.TextAlignmentTypeValues.Center },
                        new Drawing.Run(
                            new Drawing.RunProperties() { FontSize = 1200 },
                            new Drawing.Text(shapeName)
                        )
                    )
                )
            );

            return shape;
        }
    }

}


