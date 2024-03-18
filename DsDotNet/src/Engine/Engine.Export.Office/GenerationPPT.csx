//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using Dual.Common.Core;
//using Spire.Presentation;
//using Spire.Presentation.Drawing;
//using static Engine.Core.CoreModule;
//using static Engine.Core.DsType;

//namespace Engine.Export.Office
//{
//    public static class GenerationPPT
//    {
//        private static float _ShapeWidth = 110, _ShapeHeight = 40;

//        /// <summary>
//        /// 주어진 시스템의 플로우를 기반으로 PPTX 파일을 생성합니다.
//        /// </summary>
//        /// <param name="sys">시스템 객체</param>
//        /// <param name="templateFile">템플릿 파일 경로</param>
//        /// <param name="targetFile">대상 파일 경로</param>
//        /// <returns>생성된 PPTX 파일 경로</returns>
//        public static string ExportPPT(DsSystem sys, string templateFile, string targetFile)
//        {
//            // 템플릿 파일을 대상 파일로 복사
//            File.Copy(templateFile, targetFile, true);

//            using (Presentation ppt = new Presentation())
//            {
//                ppt.LoadFromFile(targetFile);

//                //첫페이지 이름을 시스템이름으로 변경 
//                UpdateFirstSlide(ppt,  sys.Name);
//                // 슬라이드 추가
//                AddSlidesWithNames(ppt,  sys.Flows);

//                // 변경사항 저장
//                ppt.SaveToFile(targetFile, FileFormat.Pptx2013);
//            }

//            return targetFile;
//        }

//        private static void UpdateFirstSlide(Presentation ppt, string name)
//        {
//            // 첫 번째 슬라이드가 존재하는지 확인
//            if (ppt.Slides.Count > 0)
//            {
//                // 첫 번째 슬라이드의 제목을 주어진 이름으로 설정
//                ISlide firstSlide = ppt.Slides[0];
//                for (int i = 0; i < firstSlide.Shapes.Count; i++)
//                {
//                    var item = firstSlide.Shapes[i] as IAutoShape;

//                    if (item != null && item.Placeholder.Type == PlaceholderType.CenteredTitle)
//                        item.TextFrame.Text = name;
//                }
//            }
//            else
//            {
//                throw new Exception("Presentation does not contain any slides.");
//            }
//        }

//        /// <summary>
//        /// 파일에 플로우 이름을 갖는 슬라이드를 추가합니다.
//        /// </summary>
//        /// <param name="filePath">파일 경로</param>
//        /// <param name="flows">플로우 컬렉션</param>
//        public static void AddSlidesWithNames(Presentation ppt, HashSet<Flow> flows)
//        {
//            ShapeType GetShapeType(Vertex v)
//            {
//                if (v is Real) return ShapeType.Rectangle;
//                if (v is Alias a) return a.TargetWrapper.IsDuAliasTargetReal ? ShapeType.Rectangle : ShapeType.Ellipse;
//                return ShapeType.Ellipse;
//            }

//            string GetName(Vertex v)
//            {
//                if (v is Real || v is RealOtherFlow) return v.Name;
//                if (v is Call c)
//                {
//                    var flowName = c.Parent.GetFlow().Name;
//                    var deviceName = c.TaskDevs.First().DeviceName;
//                    var callName = "";
//                    // Find the index where flowName starts in deviceName
//                    int startIndex = deviceName.IndexOf(flowName);
//                    if (startIndex != -1) // If flowName is found in deviceName
//                        callName = $"{deviceName.Remove(startIndex, flowName.Length).TrimStart('_')}.{c.TaskDevs.First().ApiItem.Name}";
//                    else
//                        callName = $"{deviceName}.{c.TaskDevs.First().ApiItem.Name}";


//                    if (c.TargetJob.ActionType.IsMultiAction)
//                        return $"{callName}[{c.TaskDevs.Count()}]";
//                    else
//                        return callName;
//                }

//                if (v is Alias a) return a.TargetWrapper.GetTarget().Name;
//                throw new Exception("Vertex GetName error");
//            }


//            foreach (var flow in flows)
//            {
//                // 제목만 있는 레이아웃을 사용하여 새 슬라이드 추가
//                ISlide slide = ppt.Slides.Append(SlideLayoutType.TitleOnly);

//                // 제목 자리 표시자에 플로우 이름 설정
//                IAutoShape titleShape = slide.Shapes[0] as IAutoShape;
//                if (titleShape != null && titleShape.TextFrame != null)
//                    titleShape.TextFrame.Text = flow.Name;
//                else
//                    throw new Exception("TitleOnly error");

//                // 각 플로우의 실제 이름을 슬라이드에 추가
//                float yPos = 100;
//                float maxXPos = 0; // 슬라이드에서 가장 오른쪽에 위치한 도형의 X 좌표
//                foreach (var fv in flow.Graph.Vertices)
//                {
//                    // 도형의 X 좌표
//                    float xPos = 1;

//                    // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
//                    IAutoShape fShape = slide.Shapes.AppendShape(GetShapeType(fv), new RectangleF(xPos, yPos, _ShapeWidth, _ShapeHeight));
//                    var fTextFrame = fShape.TextFrame;
//                    fTextFrame.Paragraphs[0].Text = GetName(fv);
//                    fTextFrame.TextRange.FontHeight = 11;

//                    // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
//                    maxXPos = Math.Max(maxXPos, xPos + _ShapeWidth);

//                    // yPos 업데이트
//                    yPos += 50;

//                    if (fv is Real r)
//                    {
//                        foreach (var cv in r.Graph.Vertices)
//                        {
//                            // 각 Vertex마다 높이를 조정하여 겹치지 않도록 함
//                            xPos += _ShapeWidth + 20; // 도형이 겹치지 않도록 간격 추가

//                            // 도형이 슬라이드 너비를 넘어가면 아래로 이동
//                            if (xPos + _ShapeWidth > ppt.SlideSize.Size.Width)
//                            {
//                                xPos = 1; // 왼쪽 끝으로 이동
//                                yPos += _ShapeHeight + 20; // 아래로 이동
//                            }

//                            // 도형 추가
//                            IAutoShape rShape = slide.Shapes.AppendShape(GetShapeType(cv), new RectangleF(xPos, yPos, _ShapeWidth, _ShapeHeight));
//                            var rTextFrame = rShape.TextFrame;
//                            rTextFrame.Paragraphs[0].Text = GetName(cv);
//                            rTextFrame.TextRange.FontHeight = 11;
//                            // 도형의 채우기 색상 설정
//                            rShape.Fill.FillType = FillFormatType.Solid;
//                            rShape.Fill.SolidColor.Color = Color.Blue; // PpColorSchemeIndex.ppAccent2 대신 색상을 직접 지정
//                            rShape.Line.FillType = FillFormatType.Solid;
//                            rShape.Line.SolidFillColor.Color = Color.Black;


//                            // 가장 오른쪽에 위치한 도형의 X 좌표 업데이트
//                            maxXPos = Math.Max(maxXPos, xPos + _ShapeWidth);
//                        }
//                    }
//                }

//                // 슬라이드 너비를 넘어가는 경우 아래쪽으로 이동
//                if (maxXPos > ppt.SlideSize.Size.Width)
//                    yPos += _ShapeHeight + 20;

//                // 슬라이드 높이를 조정하여 도형이 잘 보이도록 함
//                ppt.SlideSize.Size = new SizeF(ppt.SlideSize.Size.Width, yPos + _ShapeHeight + 100);
//            }

   
//        }

//    }
//}
