using System;
using System.Security.Cryptography;

namespace Dual.Common.Utils
{
    /// <summary>
    /// 대칭 키
    /// </summary>
    [Serializable]
    public class SymetricKey
    {
        private byte[] iv = null;
        private byte[] key = null;

        /// <summary>
        /// key size 
        /// </summary>
        public int KeySize = 256;

        /// <summary>
        /// get or set iv
        /// </summary>
        public string IV
        {
            get { return Convert.ToBase64String(iv); }
            set
            {
                iv = Convert.FromBase64String(value);
            }
        }

        /// <summary>
        /// get or set key
        /// </summary>
        public string Key
        {
            get
            {
                return Convert.ToBase64String(key);
            }
            set
            {
                key = Convert.FromBase64String(value);
            }
        }

        private CryptoType algorism = CryptoType.AES;
        /// <summary>
        /// 암호화 알고리즘
        /// </summary>
        public CryptoType Algorism
        {
            get
            {
                return algorism;
            }
            set
            {
                algorism = value;
            }
        }

        /// <summary>
        /// 생성
        /// </summary>
        public void Generate()
        {
            SymmetricAlgorithm crypto = null;
            switch (algorism)
            {
                case CryptoType.AES:
                    crypto = Aes.Create();
                    break;
                case CryptoType.TrpleDES:
                    crypto = TripleDES.Create();
                    break;
            }
            using (crypto)
            {
                crypto.KeySize = KeySize;
                crypto.GenerateIV();
                crypto.GenerateKey();
                iv = crypto.IV;
                key = crypto.Key;
            }
        }

    }
}
