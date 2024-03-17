using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Dual.Common.Core;
using Spire.Presentation;
using Spire.Presentation.Drawing;
using static Engine.Core.CoreModule;

namespace Engine.Export.Office
{
    public static class GenerationPPT
    {
        private static float _ShapeWidth = 110, _ShapeHeight = 40;

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

            // �����̵� �߰�
            AddSlidesWithNames(targetFile, sys.Flows);

            return targetFile;
        }

        /// <summary>
        /// ���Ͽ� �÷ο� �̸��� ���� �����̵带 �߰��մϴ�.
        /// </summary>
        /// <param name="filePath">���� ���</param>
        /// <param name="flows">�÷ο� �÷���</param>
        public static void AddSlidesWithNames(string filePath, HashSet<Flow> flows)
        {
            ShapeType GetShapeType(Vertex v)
            {
                if (v is Real) return ShapeType.Rectangle;
                if (v is Alias a) return a.TargetWrapper.IsDuAliasTargetReal ? ShapeType.Rectangle : ShapeType.Ellipse;
                return ShapeType.Ellipse;
            }

            string GetName(Vertex v)
            {
                if (v is Real || v is Call) return v.Name;
                if (v is Alias a) return a.TargetWrapper.GetTarget().Name;
                throw new Exception("Vertex GetName error");
            }

            using (Presentation ppt = new Presentation())
            {
                ppt.LoadFromFile(filePath);

                foreach (var flow in flows)
                {
                    // ���� �ִ� ���̾ƿ��� ����Ͽ� �� �����̵� �߰�
                    ISlide slide = ppt.Slides.Append(SlideLayoutType.TitleOnly);

                    // ���� �ڸ� ǥ���ڿ� �÷ο� �̸� ����
                    IAutoShape titleShape = slide.Shapes[0] as IAutoShape;
                    if (titleShape != null && titleShape.TextFrame != null)
                        titleShape.TextFrame.Text = flow.Name;
                    else
                        throw new Exception("TitleOnly error");

                    // �� �÷ο��� ���� �̸��� �����̵忡 �߰�
                    float yPos = 100;
                    float maxXPos = 0; // �����̵忡�� ���� �����ʿ� ��ġ�� ������ X ��ǥ
                    foreach (var fv in flow.Graph.Vertices)
                    {
                        // ������ X ��ǥ
                        float xPos = 1;

                        // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                        IAutoShape fShape = slide.Shapes.AppendShape(GetShapeType(fv), new RectangleF(xPos, yPos, _ShapeWidth, _ShapeHeight));
                        var fTextFrame = fShape.TextFrame;
                        fTextFrame.Paragraphs[0].Text = GetName(fv);
                        fTextFrame.TextRange.FontHeight = 11;

                        // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
                        maxXPos = Math.Max(maxXPos, xPos + _ShapeWidth);

                        // yPos ������Ʈ
                        yPos += 50;

                        if (fv is Real r)
                        {
                            foreach (var cv in r.Graph.Vertices)
                            {
                                // �� Vertex���� ���̸� �����Ͽ� ��ġ�� �ʵ��� ��
                                xPos += _ShapeWidth + 20; // ������ ��ġ�� �ʵ��� ���� �߰�

                                // ������ �����̵� �ʺ� �Ѿ�� �Ʒ��� �̵�
                                if (xPos + _ShapeWidth > ppt.SlideSize.Size.Width)
                                {
                                    xPos = 1; // ���� ������ �̵�
                                    yPos += _ShapeHeight + 20; // �Ʒ��� �̵�
                                }

                                // ���� �߰�
                                IAutoShape rShape = slide.Shapes.AppendShape(GetShapeType(cv), new RectangleF(xPos, yPos, _ShapeWidth, _ShapeHeight));
                                var rTextFrame = rShape.TextFrame;
                                rTextFrame.Paragraphs[0].Text = GetName(cv);
                                rTextFrame.TextRange.FontHeight = 11;
                                // ������ ä��� ���� ����
                                rShape.Fill.FillType = FillFormatType.Solid;
                                rShape.Fill.SolidColor.Color = Color.Blue; // PpColorSchemeIndex.ppAccent2 ��� ������ ���� ����
                                rShape.Line.FillType = FillFormatType.Solid;
                                rShape.Line.SolidFillColor.Color = Color.Black;


                                // ���� �����ʿ� ��ġ�� ������ X ��ǥ ������Ʈ
                                maxXPos = Math.Max(maxXPos, xPos + _ShapeWidth);
                            }
                        }
                    }

                    // �����̵� �ʺ� �Ѿ�� ��� �Ʒ������� �̵�
                    if (maxXPos > ppt.SlideSize.Size.Width)
                        yPos += _ShapeHeight + 20;

                    // �����̵� ���̸� �����Ͽ� ������ �� ���̵��� ��
                    ppt.SlideSize.Size = new SizeF(ppt.SlideSize.Size.Width, yPos + _ShapeHeight + 100);
                }

                // ������� ����
                ppt.SaveToFile(filePath, FileFormat.Pptx2013);
            }
        }
    }
}
