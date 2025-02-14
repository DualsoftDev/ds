// http://rextester.com/discussion/LMCV31603/Gzip-string-and-back

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GZipStream = System.IO.Compression.GZipStream;
using SysZipFile = System.IO.Compression.ZipFile;
using IonicZipFile = Ionic.Zip.ZipFile;

namespace Dual.Common.Core
{
    public static class EmZip
    {
        public static byte[] ZippedBytesFromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            return Compress(bytes);
        }

        public static void ZippedBytesToFile(string path, byte[] gzBuffer)
        {
            var bytes = Decompress(gzBuffer);
            File.WriteAllBytes(path, bytes);
        }
        public static byte[] Compress(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return Compress(buffer);
        }
        public static byte[] Compress(this byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public static string ToBase64String(byte[] gzBuffer) => Convert.ToBase64String(gzBuffer);
        public static byte[] FromBase64String(string compressedText) => Convert.FromBase64String(compressedText);

        public static byte[] Decompress(this byte[] gzBuffer)
        {
            if (gzBuffer == null || gzBuffer.Length == 0)
                return null;

            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);
                }

                return buffer;

                //return Encoding.UTF8.GetString(buffer);
            }
        }


        /// <summary>
        /// 해당 폴더 내용 전체를 zip file 로 만들어 반환
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folder"></param>
        [Obsolete]
        public static void CreateZipFileFromFolder(string fileName, string folder)
            => SysZipFile.CreateFromDirectory(folder, fileName);

        public static void ZipFileFromFolder(string folder, string zipFileName)
        {
            File.Delete(zipFileName);
            SysZipFile.CreateFromDirectory(folder, zipFileName);
        }
        /// <summary>
        /// 해당 폴더 내용 전체를 zip file 로 만들었을 때의 byte 를 반환
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static byte[] ZippedBytesFromFolder(string folder)
        {
            var zipFileName = Path.GetTempFileName();
            File.Delete(zipFileName);
            SysZipFile.CreateFromDirectory(folder, zipFileName);
            var bytes = File.ReadAllBytes(zipFileName);
            File.Delete(zipFileName);
            return bytes;
        }

        public static void ZippedBytesToFolder(byte[] zippedBytes, string folder, Ionic.Zip.ExtractExistingFileAction onExisting = Ionic.Zip.ExtractExistingFileAction.DoNotOverwrite)
        {
            var tmp = Path.GetTempFileName();
            File.WriteAllBytes(tmp, zippedBytes);

            Directory.CreateDirectory(folder);
            var backupDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folder);
            using (var zip = IonicZipFile.Read(tmp))
                zip.ExtractAll(folder, onExisting);
            File.Delete(tmp);
            Directory.SetCurrentDirectory(backupDirectory);
        }

        public static byte[] FolderToZippedBytes(string folder)
        {
            var tmp = Path.GetTempFileName();
            ZipFileFromFolder(folder, tmp);
            var bytes = File.ReadAllBytes(tmp);
            File.Delete(tmp);
            return bytes;
        }



        public static void ExtractZippedBytesToFolder(byte[] buffer, string folder)
        {
            var zipFileName = Path.GetTempFileName();
            ZippedBytesToFile(zipFileName, buffer);
            SysZipFile.ExtractToDirectory(zipFileName, folder);
            File.Delete(zipFileName);
        }

        // https://stackoverflow.com/questions/37091361/how-to-compress-multiple-files-in-zip-file
        /// <summary>
        /// Create a ZIP file of the files provided.
        /// </summary>
        /// <param name="fileName">The full path and name to store the ZIP file at.</param>
        /// <param name="files">The list of files to be added.</param>
        /// System.IO.Compression.dll 및 System.IO.Compression.FileSystem.dll 참조 추가 필요함.
        public static void CreateZipFile(string fileName, IEnumerable<string> files)
        {
            using var zip = new IonicZipFile(fileName)
            {
                AlternateEncoding = Encoding.UTF8,
                AlternateEncodingUsage = Ionic.Zip.ZipOption.Always,
            };
            foreach (var file in files)
            {
                // Add the entry for each file
                zip.AddEntry(file, File.ReadAllBytes(file));
            }
            zip.Save();

            //// Create and open a new ZIP file
            ////var zip = SysZipFile.Open(fileName, System.IO.Compression.ZipArchiveMode.Create);
            //var zip = SysZipFile.Open(fileName, System.IO.Compression.ZipArchiveMode.Create);
            //foreach (var file in files)
            //{
            //    // Add the entry for each file
            //    zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
            //}
            //// Dispose of the object when we are done
            //zip.Dispose();
        }
    }
}
