using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace Engine.Export.Office
{
    public class ShapeManager
    {
        // 도형의 크기 설정
        internal static int Width = 35;  //mm 
        internal static int Height = 12; //mm
        internal static long Emu = 36000; // 1mm는 360000 EMU(Enhanced Metafile Unit)에 해당

        static long _width = Width * Emu; // 폭 3.5cm
        static long _height =Height * Emu; // 높이 1.2cm

        internal static Shape AddSlideShape(Slide slide, string shapeName, Drawing.ShapeTypeValues shapeTypeValues, int x, int y)
        {
            var shapeTree = slide.CommonSlideData.ShapeTree;
            uint drawingObjectId = GetNextShapeId(slide);

            var shape = CreateNormalShape(shapeName, drawingObjectId, shapeTypeValues, x* Emu, y* Emu);
            shapeTree.AppendChild(shape);

            return shape;
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
            var schemeColor = shapeTypeValue == Drawing.ShapeTypeValues.Rectangle ? Drawing.SchemeColorValues.Accent1 : Drawing.SchemeColorValues.Accent3;
            var textAlignment = shapeTypeValue == Drawing.ShapeTypeValues.Rectangle ? Drawing.TextFontAlignmentValues.Top : Drawing.TextFontAlignmentValues.Bottom;
            var shape = new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = drawingObjectId, Name = $"Shape {drawingObjectId}" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                
                new ShapeProperties(
                    new Drawing.Transform2D(
                        new Drawing.Offset() { X = offsetX, Y = offsetY },
                        new Drawing.Extents() { Cx = _width, Cy = _height }),
                    new Drawing.PresetGeometry(new Drawing.AdjustValueList()) { Preset = shapeTypeValue }, // 직사각형 형태 지정
                    new Drawing.SolidFill(new Drawing.SchemeColor() { Val = schemeColor }),
                    new Drawing.Outline(new Drawing.SolidFill(new Drawing.SchemeColor() { Val = Drawing.SchemeColorValues.Dark1 }))
                    {
                        Width = 12700 // 윤곽선 두께 (단위: EMU, 예: 12700 EMU ≈ 1pt)
                    }
                ),

                new TextBody(
                    new Drawing.BodyProperties(),
                    new Drawing.ListStyle(),
                    new Drawing.Paragraph(
                        new Drawing.ParagraphProperties() { Alignment = Drawing.TextAlignmentTypeValues.Center, FontAlignment = textAlignment },
                        new Drawing.Run(
                            new Drawing.RunProperties() { FontSize = 1200 },
                            new Drawing.Text(shapeName)
                        )
                    )
                )
            );

            return shape;
        }


        public static void ConvertShapesToGroupShape(PresentationDocument presentationDocument, Slide slide, Shape[] shapes)
        {
            double groupWidth = 0, groupHeight = 0;
            double groupX = double.MaxValue, groupY = double.MaxValue;
          
            // 모든 모양들의 경계 상자를 고려하여 그룹의 너비와 높이를 계산합니다.
            foreach (var shape in shapes)
            {
                var shapeBounds = GetShapeBounds(shape);
                groupWidth = Math.Max(groupWidth, shapeBounds.Item1 + shapeBounds.Item3);
                groupHeight = Math.Max(groupHeight, shapeBounds.Item2 + shapeBounds.Item4);
                groupX = Math.Min(groupX, shapeBounds.Item1);
                groupY = Math.Min(groupY, shapeBounds.Item2);
            }

            // 그룹 모양을 만듭니다.
            GroupShape groupShape = new GroupShape();
            groupShape.NonVisualGroupShapeProperties = new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties() { Id = (UInt32Value)1001U, Name = "Group 1" },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()
            );
          

            GroupShapeProperties groupShapeProperties = new GroupShapeProperties();
            groupShape.Append(groupShapeProperties);


            var groupOffset = new Drawing.Offset() { X = Convert.ToInt64(groupX), Y = Convert.ToInt64(groupY) }; // 위치 설정
            var groupExtents = new Drawing.Extents() { Cx = Convert.ToInt64(groupWidth), Cy = Convert.ToInt64(groupHeight) }; // 크기 설정
            groupShapeProperties.TransformGroup = new Drawing.TransformGroup(
              groupOffset,
              groupExtents,
                new Drawing.ChildOffset() { X = Convert.ToInt64(groupX), Y = Convert.ToInt64(groupY) },
                new Drawing.ChildExtents() { Cx = Convert.ToInt64(groupWidth), Cy = Convert.ToInt64(groupHeight) } // 자식 요소 크기 설정
            );
            
            shapes.First().ShapeProperties.Transform2D = new Drawing.Transform2D() {
                Offset = new Drawing.Offset() { X = Convert.ToInt64(groupX), Y = Convert.ToInt64(groupY) },
                Extents = new Drawing.Extents() { Cx = Convert.ToInt64(groupWidth), Cy = Convert.ToInt64(groupHeight) }
                 };

            // 선택된 모양들을 그룹에 추가합니다.
            foreach (var shape in shapes)
            {
                groupShape.Append(shape.CloneNode(true));
                shape.Remove();
            }

            // 슬라이드에 그룹 모양을 추가합니다.
            slide.CommonSlideData.ShapeTree.Append(groupShape);
        }


        // 모양의 경계 상자와 위치를 가져오는 도우미 메서드입니다.
        private static Tuple<double, double, double, double> GetShapeBounds(Shape shape)
        {
            var shapeProperties = shape.ShapeProperties;

            // 모양의 위치와 크기를 가져옵니다.
            var offset = shapeProperties.Transform2D.Offset;
            var extents = shapeProperties.Transform2D.Extents;

            double x = offset.X;
            double y = offset.Y;
            double width = extents.Cx ;
            double height = extents.Cy;

            return new Tuple<double, double, double, double>(x, y, width, height);
        }
    }

}


