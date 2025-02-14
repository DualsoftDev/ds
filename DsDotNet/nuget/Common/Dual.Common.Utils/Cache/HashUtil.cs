
using System;
using System.Text;

using System.Security.Cryptography;

namespace Dual.Common.Utils
{
    /// <summary>
    /// 해시 알고리즘을 지원합니다.
    /// </summary>
    public class HashUtil
    {
        /// <summary>
        /// 해시 기능을 계산합니다.
        /// </summary>
        /// <param name="type">해시 알고리즘</param>
        /// <param name="data">데이터</param>
        /// <returns>해시된 값</returns>
        public static byte[] ComputHash(HashType type, byte[] data)
        {
            byte[] result = null;

            HashAlgorithm algorism = null;
            switch (type)
            {
                case HashType.MD5:
                    algorism = MD5.Create();
                    break;
                case HashType.SHA1:
                    algorism = SHA1.Create();
                    break;
            }

            using (algorism)
            {
                result = algorism.ComputeHash(data);
            }

            return result;
        }

        /// <summary>
        /// 해시 기능을 계산합니다.
        /// </summary>
        /// <param name="type">해시 알고리즘</param>
        /// <param name="text">데이터</param>
        /// <returns>해시된 값</returns>
        public static string ComputHash(HashType type, string text)
        {
            byte[] input = Encoding.Default.GetBytes(text);
            byte[] output = ComputHash(type, input);
            string result = Convert.ToBase64String(output);
            return result;
        }
    }
}