using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace Engine.Export.Office
{
    public class PageManager
    {
        public static void UpdateFirstPageTitle(PresentationDocument presentationDocument, string slideTitle)
        {
            SlidePart firstSlidePart = presentationDocument.PresentationPart.SlideParts.First();
            Shape titleShape = firstSlidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
                                .First(s=>s.NonVisualShapeProperties.ApplicationNonVisualDrawingProperties.PlaceholderShape.Type == PlaceholderValues.CenteredTitle);

            TextBody textBody = titleShape.Descendants<TextBody>().FirstOrDefault();
            Drawing.Paragraph paragraph = textBody.Descendants<Drawing.Paragraph>().FirstOrDefault();
            
            Drawing.Run run = paragraph.Descendants<Drawing.Run>().FirstOrDefault();
            run.Text = new Drawing.Text(slideTitle);
        }

        public static SlidePart InsertNewSlideWithTitleOnly(PresentationDocument presentationDocument, Slide slide ,  string slideTitle)
        {
            PresentationPart presentationPart = presentationDocument.PresentationPart;

            if (presentationPart == null)
            {
                throw new InvalidOperationException("The presentation document is empty.");
            }

            uint drawingObjectId = 1;

            CommonSlideData commonSlideData = slide.CommonSlideData ?? slide.AppendChild(new CommonSlideData());
            ShapeTree shapeTree = commonSlideData.ShapeTree ?? commonSlideData.AppendChild(new ShapeTree());
            NonVisualGroupShapeProperties nonVisualProperties = shapeTree.AppendChild(new NonVisualGroupShapeProperties());
            nonVisualProperties.NonVisualDrawingProperties = new NonVisualDrawingProperties() { Id = 1, Name = "" };
            nonVisualProperties.NonVisualGroupShapeDrawingProperties = new NonVisualGroupShapeDrawingProperties();
            nonVisualProperties.ApplicationNonVisualDrawingProperties = new ApplicationNonVisualDrawingProperties();

            shapeTree.AppendChild(new GroupShapeProperties());


            Shape titleShape = ShapeManager.CreateTitleShape(slideTitle);
            shapeTree.AppendChild(titleShape); ;
            drawingObjectId++;

            titleShape.NonVisualShapeProperties = new NonVisualShapeProperties
            (
                new NonVisualDrawingProperties() { Id = drawingObjectId, Name = "Title" },
                new NonVisualShapeDrawingProperties(new Drawing.ShapeLocks() { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties(new PlaceholderShape() { Type = PlaceholderValues.Title })
            );
            titleShape.ShapeProperties = new ShapeProperties();

            titleShape.TextBody = new TextBody(new Drawing.BodyProperties(),
                new Drawing.ListStyle(),
                new Drawing.Paragraph(new Drawing.Run(new Drawing.Text() { Text = slideTitle }))
            );

            SlideMasterPart slideMasterPart = presentationPart.SlideMasterParts.First();
            SlideLayoutPart slideLayoutPart = slideMasterPart.SlideLayoutParts.FirstOrDefault(
                slp => slp.SlideLayout.Type.InnerText == "titleOnly");

            // 특정 레이아웃을 찾지 못한 경우 예외 처리
            if (slideLayoutPart == null)
            {
                throw new InvalidOperationException("Required slide layout 'Title Slide' was not found.");
            }

            // 새 슬라이드 파트 생성 및 기존 레이아웃 파트를 추가
            SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.AddPart(slideLayoutPart);

            SlideIdList slideIdList = presentationPart.Presentation.SlideIdList;
            SlideId lastSlideId = slideIdList.Elements<SlideId>().Last();
            SlideId newSlideId = slideIdList.InsertAfter(new SlideId(), lastSlideId);
            newSlideId.Id = lastSlideId.Id + 1;
            newSlideId.RelationshipId = presentationPart.GetIdOfPart(slidePart);

            return slidePart;
        }
    }
}


