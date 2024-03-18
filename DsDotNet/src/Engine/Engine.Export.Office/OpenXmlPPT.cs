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
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;

namespace Engine.Export.Office
{
    public static class OpenXmlPPT
    {

        /// <summary>
        /// 주어진 시스템의 플로우를 기반으로 PPTX 파일을 생성합니다.
        /// </summary>
        /// <param name="sys">시스템 객체</param>
        /// <param name="templateFile">템플릿 파일 경로</param>
        /// <param name="targetFile">대상 파일 경로</param>
        /// <returns>생성된 PPTX 파일 경로</returns>
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


        ///// <summary>
        ///// 파일에 플로우 이름을 갖는 슬라이드를 추가합니다.
        ///// </summary>
        ///// <param name="filePath">파일 경로</param>
        ///// <param name="flows">플로우 컬렉션</param>
        public static void AddSlidesWithFlow(PresentationDocument doc, Flow flow)
        {
            Drawing.ShapeTypeValues GetShapeType(Vertex v)
            {
                if (v is Real) return Drawing.ShapeTypeValues.Rectangle;
                if (v is Alias a) return a.TargetWrapper.IsDuAliasTargetReal ? Drawing.ShapeTypeValues.Rectangle : Drawing.ShapeTypeValues.Ellipse;
                return Drawing.ShapeTypeValues.Ellipse;
            }

            string GetName(Vertex v)
            {
                if (v is Real || v is RealOtherFlow) return v.Name;
                if (v is Call c)
                {
                    var flowName = c.Parent.GetFlow().Name;
                    var deviceName = c.TaskDevs.First().DeviceName;
                    var callName = "";
                    // Find the index where flowName starts in deviceName
                    int startIndex = deviceName.IndexOf(flowName);
                    if (startIndex != -1) // If flowName is found in deviceName
                        callName = $"{deviceName.Remove(startIndex, flowName.Length).TrimStart('_')}\n.{c.TaskDevs.First().ApiItem.Name}";
                    else
                        callName = $"{deviceName}\n.{c.TaskDevs.First().ApiItem.Name}";


                    if (c.TargetJob.ActionType.IsMultiAction)
                        return $"{callName}[{c.TaskDevs.Count()}]";
                    else
                        return callName;
                }

                if (v is Alias a) return a.TargetWrapper.GetTarget().Name;
                throw new Exception("Vertex GetName error");
            }
            Slide slide = new Slide(new CommonSlideData(new ShapeTree()));
            var slidePart = PageManager.InsertNewSlideWithTitleOnly(doc, slide, flow.Name);
            var slideWidth = doc.PresentationPart.Presentation.SlideSize.Cx / ShapeManager.Emu;

            // 각 플로우의 실제 이름을 슬라이드에 추가
            int yPos = 50;
            int maxXPos = 0; // 슬라이드에서 가장 오른쪽에 위치한 도형의 X 좌표
            foreach (var fv in flow.Graph.Vertices)
            {
                // 도형의 X 좌표
                int xPos = 1;

                // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
                Shape fShape = ShapeManager.AddSlideShape(slide, GetName(fv), GetShapeType(fv), xPos, yPos);

                // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
                maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width); 

                // yPos 업데이트
                yPos += 40;


                List<Shape> groupItems = new List<Shape>() { fShape };
                if (fv is Real r)
                {
                    foreach (var cv in r.Graph.Vertices)
                    {
                        // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
                        xPos += ShapeManager.Width + 15; // 도형이 겹치지 않도록 간격 추가
          
                        // 도형이 슬라이드 너비를 넘어가면 아래로 이동
                        if (xPos + ShapeManager.Width > slideWidth)
                        {
                            xPos = 1; // 왼쪽 끝으로 이동
                            yPos += ShapeManager.Height + 10; // 아래로 이동
                        }

                        Shape rShape = ShapeManager.AddSlideShape(slide, GetName(cv), GetShapeType(cv), xPos, yPos);
                        groupItems.Add(rShape);
                         // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
                         maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width);
                    }
                }

                if (groupItems.Count > 1)
                    ShapeManager.ConvertShapesToGroupShape(doc, slide, groupItems.ToArray());          

                slide.Save(slidePart);
            }
        }

    }
}
