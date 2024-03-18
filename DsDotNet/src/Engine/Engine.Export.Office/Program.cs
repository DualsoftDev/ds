using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;

namespace PresentationUtility
{
    public class PresentationManagerDemo
    {
        public static void Main(string[] args)
        {
            var samplePath = @"E:\DSTemplate.pptx";
            var systemName = "testsysA";

            string datetime = DateTime.Now.ToString("yyMMdd HH-mm-ss", CultureInfo.InvariantCulture);
            string destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"temp/dualsoft/{datetime}");

            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            string destinationFilePath = Path.Combine(destinationFolder, $"{systemName}.pptx");
            File.Copy(samplePath, destinationFilePath, true);
            using (PresentationDocument doc = PresentationDocument.Open(destinationFilePath, true))
            {
                PageManager.UpdateFirstPageTitle(doc, systemName);

                for (int i = 0; i < 7; i++)
                {
                        PageManager.InsertNewSlideWithTitleOnly(doc, $"page{i}");
                }

                doc.Save();
            }

            Process.Start(new ProcessStartInfo { FileName = destinationFilePath, UseShellExecute = true });
        }

      
    }
}
