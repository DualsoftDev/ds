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
        /// �־��� �ý����� �÷ο츦 ������� PPTX ������ �����մϴ�.
        /// </summary>
        /// <param name="sys">�ý��� ��ü</param>
        /// <param name="templateFile">���ø� ���� ���</param>
        /// <param name="targetFile">��� ���� ���</param>
        /// <returns>������ PPTX ���� ���</returns>
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


        ///// <summary>
        ///// ���Ͽ� �÷ο� �̸��� ���� �����̵带 �߰��մϴ�.
        ///// </summary>
        ///// <param name="filePath">���� ���</param>
        ///// <param name="flows">�÷ο� �÷���</param>
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

            // �� �÷ο��� ���� �̸��� �����̵忡 �߰�
            int yPos = 50;
            int maxXPos = 0; // �����̵忡�� ���� �����ʿ� ��ġ�� ������ X ��ǥ
            foreach (var fv in flow.Graph.Vertices)
            {
                // ������ X ��ǥ
                int xPos = 1;

                // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                Shape fShape = ShapeManager.AddSlideShape(slide, GetName(fv), GetShapeType(fv), xPos, yPos);

                // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
                maxXPos = Math.Max(maxXPos, xPos + ShapeManager.Width); 

                // yPos ������Ʈ
                yPos += 40;


                List<Shape> groupItems = new List<Shape>() { fShape };
                if (fv is Real r)
                {
                    foreach (var cv in r.Graph.Vertices)
                    {
                        // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                        xPos += ShapeManager.Width + 15; // ������ ��ġ�� �ʵ��� ���� �߰�
          
                        // ������ �����̵� �ʺ� �Ѿ�� �Ʒ��� �̵�
                        if (xPos + ShapeManager.Width > slideWidth)
                        {
                            xPos = 1; // ���� ������ �̵�
                            yPos += ShapeManager.Height + 10; // �Ʒ��� �̵�
                        }

                        Shape rShape = ShapeManager.AddSlideShape(slide, GetName(cv), GetShapeType(cv), xPos, yPos);
                        groupItems.Add(rShape);
                         // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
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
