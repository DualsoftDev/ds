using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

using Drawing = DocumentFormat.OpenXml.Drawing;

using Dual.Common.Core;
using Engine.Core;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.DsText;
using static Engine.Core.DsConstants;
using static Engine.Core.CoreModule.SystemModule;
using static Engine.Core.CoreModule.GraphItemsModule;

namespace Engine.Export.Office
{
    public static class OpenXmlPPT
    {
        private static int _startXPos = 30;
        private static int _startYPos = 50;

        public static string ExportPPT(DsSystem sys, string templateFile, string targetFile)
        {
            // 템플릿 파일을 대상 파일로 복사
            File.Copy(templateFile, targetFile, true);

            using (PresentationDocument doc = PresentationDocument.Open(targetFile, true))
            {
                //첫페이지 이름을 시스템이름으로 변경 
                PageManager.UpdateFirstPageTitle(doc, sys.Name);
                // 슬라이드 추가
                foreach (var flow in sys.Flows)
                {
                    AddSlidesWithFlow(doc, flow);
                }

                // 변경사항 저장
                doc.Save();
            }


            return targetFile;
        }
        private static Drawing.ShapeTypeValues GetShapeType(Vertex v)
        {
            if (v is Real) return Drawing.ShapeTypeValues.Rectangle;
            if (v is Alias a) return a.TargetWrapper.IsDuAliasTargetReal ? Drawing.ShapeTypeValues.Rectangle : Drawing.ShapeTypeValues.Ellipse;
            return Drawing.ShapeTypeValues.Ellipse;
        }

        private static string GetName(Vertex v)
        {
            if (v is Real) return v.Name;
            if (v is Call c)
            {
                var flowName = c.Parent.GetFlow().Name;
                var deviceName = c.TargetJob.TaskDefs.First().DeviceName;
                var callName = "";
                // Find the index where flowName starts in deviceName
                int startIndex = deviceName.IndexOf(flowName);
                if (startIndex != -1) // If flowName is found in deviceName
                    callName = $"{deviceName.Remove(startIndex, flowName.Length).TrimStart('_')}\n.{c.TargetJob.TaskDefs.First().ApiItem.Name}";
                else
                    callName = $"{deviceName}\n.{c.TargetJob.TaskDefs.First().ApiItem.Name}";


                if (c.TargetJob.TaskDevCount > 1)
                    return $"{callName}[{c.TargetJob.TaskDefs.Count()}]";
                else
                    return callName;
            }

            if (v is Alias a) return a.TargetWrapper.GetTarget().Name;
            throw new Exception("Vertex GetName error");
        }

        public static void AddSlidesWithFlow(PresentationDocument doc, Flow flow)
        {
            Slide slide = new Slide(new CommonSlideData(new ShapeTree()));
            var slidePart = PageManager.InsertNewSlideWithTitleOnly(doc, slide, flow.Name);
            var slideWidth = doc.PresentationPart.Presentation.SlideSize.Cx / ShapeManager.Emu;

            // 각 플로우의 실제 이름을 슬라이드에 추가
            // 도형의 X 좌표
            int xPos = _startXPos;
            int yPos = _startYPos;
            int maxXPos = 0; // 슬라이드에서 가장 오른쪽에 위치한 도형의 X 좌표
            //foreach (var fEdge in flow.Graph.Islands)
            foreach (var fEdge in flow.ModelingEdges)
            {
                AddEdge(slide, slideWidth, fEdge, ref xPos, ref yPos);
            }

            foreach (var fv in flow.Graph.Vertices)
            {
                // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
                Shape fShape = ShapeManager.AddSlideShape(slide, GetName(fv), GetShapeType(fv), xPos, yPos);

                // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
                maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width);

                // xPos 업데이트
                xPos += 50;
                updatePosition(slideWidth, ref xPos, ref yPos);

                List<Shape> groupItems = new List<Shape>() { fShape };
                if (fv is Real r)
                {
                    foreach (var cv in r.Graph.Vertices)
                    {
                        // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
                        xPos += ShapeManager.Width + 15; // 도형이 겹치지 않도록 간격 추가
                        updatePosition(slideWidth, ref xPos, ref yPos);

                        Shape rShape = ShapeManager.AddSlideShape(slide, GetName(cv), GetShapeType(cv), xPos, yPos);
                        groupItems.Add(rShape);
                        // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
                        maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width);
                    }
                }


                if (groupItems.Count > 1)
                    ShapeManager.ConvertShapesToGroupShape(doc, slide, groupItems.ToArray());

            }
            slide.Save(slidePart);


        }
        private static void updatePosition(long slideWidth, ref int xPos, ref int yPos)
        {
            // xPos 업데이트
            xPos += 50;
            // 도형이 슬라이드 너비를 넘어가면 아래로 이동
            if (xPos + ShapeManager.Width > slideWidth)
            {
                xPos = _startXPos; // 왼쪽 끝으로 이동
                yPos += ShapeManager.Height + 10; // 아래로 이동
            }
        }
        private static void AddEdge(Slide slide, long slideWidth, ModelingEdgeInfo<Vertex> fEdge, ref int xPos, ref int yPos)
        {
            var src = fEdge.Sources.First();
            var tgt = fEdge.Targets.First();
            Shape srcShape = ShapeManager.AddSlideShape(slide, GetName(src), GetShapeType(src), xPos, yPos);
            updatePosition(slideWidth, ref xPos, ref yPos);
            Shape tgtShape = ShapeManager.AddSlideShape(slide, GetName(tgt), GetShapeType(tgt), xPos, yPos);
            updatePosition(slideWidth, ref xPos, ref yPos);

            ConnectionManager.CreateConnectionShape(slide, srcShape, tgtShape, fEdge.EdgeType);
        }
    }
}
