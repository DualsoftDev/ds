using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class EmFile
    {
        // https://github.com/ravibpatel/AutoUpdater.NET/blob/master/AutoUpdater.NET/DownloadUpdateDialog.cs
        public static string ComputeChecksum(byte[] buffer)
        {
            using var hashAlgorithm = MD5.Create();
            var hash = hashAlgorithm.ComputeHash(buffer);
            var fileChecksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return fileChecksum;
        }
        public static string ComputeChecksum(string filePath) => ComputeChecksum(File.ReadAllBytes(filePath));

        public static (long, string) GetFileSizeAndChecksum(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var checksum = ComputeChecksum(bytes);
            return (bytes.Length, checksum);
        }
    }
}
