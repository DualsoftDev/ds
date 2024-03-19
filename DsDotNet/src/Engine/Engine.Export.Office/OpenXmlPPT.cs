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
using Engine.Core;
using static Engine.Core.DsText;

namespace Engine.Export.Office
{
    public static class OpenXmlPPT
    {
        private static int _startXPos = 30;
        private static int _startYPos = 50;

        public static string ExportPPT(DsSystem sys, string templateFile, string targetFile)
        {
            // ���ø� ������ ��� ���Ϸ� ����
            File.Copy(templateFile, targetFile, true);

            using (PresentationDocument doc = PresentationDocument.Open(targetFile, true))
            {
                //ù������ �̸��� �ý����̸����� ���� 
                PageManager.UpdateFirstPageTitle(doc, sys.Name);
                // �����̵� �߰�
                foreach (var flow in sys.Flows)
                {
                    AddSlidesWithFlow(doc, flow);
                }

                // ������� ����
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

        public static void AddSlidesWithFlow(PresentationDocument doc, Flow flow)
        {
            Slide slide = new Slide(new CommonSlideData(new ShapeTree()));
            var slidePart = PageManager.InsertNewSlideWithTitleOnly(doc, slide, flow.Name);
            var slideWidth = doc.PresentationPart.Presentation.SlideSize.Cx / ShapeManager.Emu;

            // �� �÷ο��� ���� �̸��� �����̵忡 �߰�
            // ������ X ��ǥ
            int xPos = _startXPos;
            int yPos = _startYPos;
            int maxXPos = 0; // �����̵忡�� ���� �����ʿ� ��ġ�� ������ X ��ǥ
            //foreach (var fEdge in flow.Graph.Islands)
            foreach (var fEdge in flow.ModelingEdges)
            {
                AddEdge(slide, slideWidth, fEdge, ref xPos, ref yPos);
            }

            foreach (var fv in flow.Graph.Vertices)
            {
                continue;
                // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                Shape fShape = ShapeManager.AddSlideShape(slide, GetName(fv), GetShapeType(fv), xPos, yPos);

                // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
                maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width);

                // xPos ������Ʈ
                xPos += 50;
                updatePosition(slideWidth, ref xPos, ref yPos);

                List<Shape> groupItems = new List<Shape>() { fShape };
                if (fv is Real r)
                {
                    foreach (var cv in r.Graph.Vertices)
                    {
                        // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                        xPos += ShapeManager.Width + 15; // ������ ��ġ�� �ʵ��� ���� �߰�
                        updatePosition(slideWidth, ref xPos, ref yPos);

                        Shape rShape = ShapeManager.AddSlideShape(slide, GetName(cv), GetShapeType(cv), xPos, yPos);
                        groupItems.Add(rShape);
                        // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
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
            // xPos ������Ʈ
            xPos += 50;
            // ������ �����̵� �ʺ� �Ѿ�� �Ʒ��� �̵�
            if (xPos + ShapeManager.Width > slideWidth)
            {
                xPos = _startXPos; // ���� ������ �̵�
                yPos += ShapeManager.Height + 10; // �Ʒ��� �̵�
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

            bool bRev =
                fEdge.EdgeSymbol == TextStartEdgeRev || fEdge.EdgeSymbol == TextStartEdgeRev
                || fEdge.EdgeSymbol == TextResetEdgeRev || fEdge.EdgeSymbol == TextResetEdgeRev
                || fEdge.EdgeSymbol == TextStartResetRev;
            var edgeName = bRev ? fEdge.EdgeRevSymbol.ToModelEdge() : fEdge.EdgeSymbol.ToModelEdge();

            ConnectionManager.CreateConnectionShape(slide, srcShape, tgtShape, edgeName, bRev);
        }
    }
}
