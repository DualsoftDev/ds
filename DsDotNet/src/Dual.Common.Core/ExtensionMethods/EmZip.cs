// http://rextester.com/discussion/LMCV31603/Gzip-string-and-back

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;


namespace Dsu.Common.Utilities.Core.ExtensionMethods
{
    public static class EmZip
    {
        public static byte[] ZippedBytesFromFile(string path)
        {
            var bytes = System.IO.File.ReadAllBytes(path);
            return Compress(bytes);
        }

        public static void ZippedBytesToFile(string path, byte[] gzBuffer)
        {
            var bytes = Decompress(gzBuffer);
            System.IO.File.WriteAllBytes(path, bytes);
        }

#if RISK_SONARQUBE
        public static byte[] Compress(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return Compress(buffer);
        }
#endif
        public static byte[] Compress(this byte[] buffer)
        {
            using var ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;

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
                return Array.Empty<byte>();

            using var ms = new MemoryStream();
            int msgLength = BitConverter.ToInt32(gzBuffer, 0);
            ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

            byte[] buffer = new byte[msgLength];

            ms.Position = 0;
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
            {
                zip.Read(buffer, 0, buffer.Length);
            }

            return buffer;

            // :: return Encoding.UTF8.GetString(buffer);
        }


        /// <summary>
        /// 해당 폴더 내용 전체를 zip file 로 만들어 반환
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="folder"></param>
        [Obsolete("Use ZipFile.CreateFromDirectory() instead.")]
        public static void CreateZipFileFromFolder(string fileName, string folder)
            => System.IO.Compression.ZipFile.CreateFromDirectory(folder, fileName);

        public static void ZipFileFromFolder(string folder, string zipFileName)
        {
            File.Delete(zipFileName);
            System.IO.Compression.ZipFile.CreateFromDirectory(folder, zipFileName);
        }
        /// <summary>
        /// 해당 폴더 내용 전체를 zip file 로 만들었을 때의 byte 를 반환
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static byte[] ZippedBytesFromFolder(string folder)
        {
            using var tmpfileCreator = new TempFile(".zip");
            var zipFileName = tmpfileCreator.Name;

            System.IO.Compression.ZipFile.CreateFromDirectory(folder, zipFileName);
            var bytes = System.IO.File.ReadAllBytes(zipFileName);
            return bytes;
        }

        public static byte[] ToBytes(this Ionic.Zip.ZipFile zip)
        {
            // Ionic.Zip.ZipFile 을 byte[] 로 변환
            using var output = new MemoryStream();
            zip.Save(output);
            return output.ToArray();
        }


        /// <summary>
        /// 복수개의 folder 를 압축해서 하나의 zip 으로 만든 byte 를 반환
        ///
        /// folderSpecs 입력이 [("C:/dir1/dir2", "dir12"), ("C:/dir1/dir3", "dir13")] 로 주어지면 zip 내용에는 dir12, dir13 폴더 아래에 모두 수집
        /// </summary>
        public static byte[] ZippedBytesFromFolders(IEnumerable<(string, string)> folderSpecs)
        {
            using var zip = new Ionic.Zip.ZipFile();
            foreach ( (var folder, var repr) in folderSpecs)
                foreach ( var f in Directory.GetFiles(folder, "*", SearchOption.AllDirectories) )
                    zip.AddFile(f, repr);
            return zip.ToBytes();
        }



        public static void ZippedBytesToFolder(byte[] zippedBytes, string folder, Ionic.Zip.ExtractExistingFileAction onExisting = Ionic.Zip.ExtractExistingFileAction.DoNotOverwrite)
        {
            if ( zippedBytes == null
                || (zippedBytes.Length == 1 && zippedBytes[0] == 0) )
            {
                Console.WriteLine("ZippedBytesToFolder() : Ignoring null or zero bytes.");
            }
            else
            {
                using var tmpfileCreator = new TempFile(".zip");
                var tmp = tmpfileCreator.Name;
                File.WriteAllBytes(tmp, zippedBytes);

                Directory.CreateDirectory(folder);
                using (var zip = Ionic.Zip.ZipFile.Read(tmp))
                {
                    zip.ExtractAll(folder, onExisting);
                }
            }
        }

        public static byte[] FolderToZippedBytes(string folder)
        {
            using var tmpfileCreator = new TempFile(".zip");
            var tmp = tmpfileCreator.Name;
            ZipFileFromFolder(folder, tmp);
            var bytes = File.ReadAllBytes(tmp);
            return bytes;
        }



        public static void ExtractZippedBytesToFolder(byte[] buffer, string folder)
        {
            using var tmpfileCreator = new TempFile(".zip");
            var zipFileName = tmpfileCreator.Name;
            ZippedBytesToFile(zipFileName, buffer);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, folder);
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
            // Create and open a new ZIP file
            var zip = ZipFile.Open(fileName, ZipArchiveMode.Create);
            foreach (var file in files)
            {
                // Add the entry for each file
                zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
            }
            // Dispose of the object when we are done
            zip.Dispose();
        }
    }
}
