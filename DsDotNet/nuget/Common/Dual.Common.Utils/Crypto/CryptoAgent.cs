using System;
using System.Security.Cryptography;
using System.Text;

namespace Dual.Common.Utils
{
    /// <summary>
    /// 암호화,복호화를 담당합니다.
    /// </summary>
    public class CryptoAgent
    {
        /// <summary>
        /// 암호화
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Encrypt(SymetricKey key, byte[] data)
        {
            SymmetricAlgorithm algorism = null;
            byte[] result = null;

            switch (key.Algorism)
            {
                case CryptoType.AES:
                    algorism = Aes.Create();
                    break;

                case CryptoType.TrpleDES:
                    algorism = TripleDES.Create();
                    break;
            }

            using (algorism)
            {
                algorism.KeySize = key.KeySize;
                algorism.IV = Convert.FromBase64String(key.IV);
                algorism.Key = Convert.FromBase64String(key.Key);

                result = algorism.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
            }

            return result;
        }

        /// <summary>
        /// 암호화
        /// </summary>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public string Encrypt(SymetricKey key, string text)
        {
            byte[] input = Encoding.Default.GetBytes(text);
            byte[] output = Encrypt(key, input);
            string result = Convert.ToBase64String(output);
            return result;
        }

        /// <summary>
        /// 복호화
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Decrypt(SymetricKey key, byte[] data)
        {
            SymmetricAlgorithm algorism = null;
            byte[] result = null;

            switch (key.Algorism)
            {
                case CryptoType.AES:
                    algorism = Aes.Create();
                    break;

                case CryptoType.TrpleDES:
                    algorism = TripleDES.Create();
                    break;
            }

            using (algorism)
            {
                algorism.KeySize = key.KeySize;
                algorism.IV = Convert.FromBase64String(key.IV);
                algorism.Key = Convert.FromBase64String(key.Key);

                result = algorism.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
            }

            return result;
        }
        /// <summary>
        /// 복호화
        /// </summary>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public string Decrypt(SymetricKey key, string text)
        {
            byte[] input = Convert.FromBase64String(text);
            byte[] output = Decrypt(key, input);
            string result = Encoding.Default.GetString(output);
            return result;
        }
    }
}

