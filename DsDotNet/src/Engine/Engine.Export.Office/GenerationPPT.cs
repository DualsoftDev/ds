using System;
using Spire.Presentation;
using System.Collections.Generic;
using Engine.CodeGenCPU;
using static Engine.Core.CoreModule;
using System.IO;
using System.Globalization;

namespace Engine.Export.Office
{
    public static class GenerationPPT
    {
        public static string ExportPPT(DsSystem sys, string templateFile)
        {
            string datetime = DateTime.Now.ToString("yyMMdd HH-mm-ss", CultureInfo.InvariantCulture);
            string destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"temp/dualsoft/{datetime}");

            // Ensure the destination folder exists, create if not.
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            // Define the destination path
            string destinationFilePath = Path.Combine(destinationFolder, $"{sys.Name}.pptx");
            // Copy the file to the destination folder
            File.Copy(templateFile, destinationFilePath, true);

            // 기존 프레젠테이션(템플릿) 복사
            System.IO.File.Copy(templateFile, destinationFilePath, overwrite: true);

            AddSlidesWithNames(destinationFilePath, new List<string>() { "F1", "F2", "F3" });

            return destinationFilePath;
        }

        public static void AddSlidesWithNames(string filePath, List<string> slideNames)
        {
            using (Presentation ppt = new Presentation())
            {
                ppt.LoadFromFile(filePath);

                foreach (var name in slideNames)
                {
                    // 새 슬라이드 추가 (제목만 있는 레이아웃 사용)
                    ISlide slide = ppt.Slides.Append(SlideLayoutType.TitleOnly);

                    // 제목 자리 표시자에 텍스트 설정
                    IAutoShape titleShape = slide.Shapes[0] as IAutoShape;
                    if (titleShape != null && titleShape.TextFrame != null)
                        titleShape.TextFrame.Text = name;
                    else
                        throw new Exception("TitleOnly error");
                }

                // 변경사항 저장
                ppt.SaveToFile(filePath, FileFormat.Pptx2013);
            }
        }
    }
}
