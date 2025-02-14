namespace Dual.Common.Utils
{
    /// <summary>
    /// 암호화/복호화를 담당합니다.
    /// </summary>
    public class CryptoUtil
    {
        /// <summary>
        /// 기본 암호화 알고리즘 - aes
        /// </summary>
        private static CryptoType defaultAlgorism = CryptoType.AES;

        /// <summary>
        /// 기본 암호화 iv
        /// </summary>
        private static string defaultIV = "eDd43fmkToFiFUewAbbbcg==";

        /// <summary>
        /// 기본 암호화 키
        /// </summary>
        private static string defaultKey = "3tU069qflQKMBfRV/UyQsTg8etzoDfaiIqKPbsB9Kqs=";

        /// <summary>
        /// 기본 암호화 키 크기 - 256
        /// </summary>
        private static int defaultKeySize = 256;


        #region Key
        private static SymetricKey key = null;

        /// <summary>
        /// 기본 키
        /// </summary>
        public static SymetricKey Key
        {
            get
            {
                if (key == null)
                {
                    key = new SymetricKey();
                    key.Algorism = defaultAlgorism;
                    key.KeySize = defaultKeySize;
                    key.IV = defaultIV;
                    key.Key = defaultKey;
                }
                return key;
            }
        }
        #endregion

        private static CryptoAgent manager = null;

        /// <summary>
        /// 암호화를 수행합니다. ( 기본키가 사용됩니다. )
        /// </summary>
        /// <param name="text">평문</param>
        /// <returns></returns>
        public static string Encrypt(string text)
        {
            if (manager == null)
            {
                manager = new CryptoAgent();
            }
            return manager.Encrypt(Key, text);
        }

        /// <summary>
        /// 복호화를 수행합니다. ( 기본키가 사용됩니다. )
        /// </summary>
        /// <param name="data">암호문</param>
        /// <returns></returns>
        public static string Decrypt(string data)
        {
            if (manager == null)
            {
                manager = new CryptoAgent();
            }
            return manager.Decrypt(Key, data);
        }
    }
}


