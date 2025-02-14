// http://www.codeproject.com/Articles/6690/Cryptography-in-C

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace Dual.Common.Utils
{
	/// <summary>
	/// Summary description for Crypto.
	/// </summary>
	public sealed class Crypto
	{
        private static string Crypt(string strData, string strPassword, bool bEncrypt)
        {
            byte[] u8_Salt = new byte[] { 0x26, 0x19, 0x81, 0x4E, 0xA0, 0x6D, 0x95, 0x34, 0x26, 0x75, 0x64, 0x05, 0xF6 };

            PasswordDeriveBytes i_Pass = new PasswordDeriveBytes(strPassword, u8_Salt);

            Aes algorithm = Aes.Create();
            algorithm.Key = i_Pass.GetBytes(32);
            algorithm.IV = i_Pass.GetBytes(16);

            ICryptoTransform transform = (bEncrypt) ? algorithm.CreateEncryptor() : algorithm.CreateDecryptor();

            MemoryStream memStream = new MemoryStream();
            CryptoStream crStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);

            byte[] u8_Data = bEncrypt ? Encoding.Unicode.GetBytes(strData) : Convert.FromBase64String(strData);

            try
            {
                crStream.Write(u8_Data, 0, u8_Data.Length);
                crStream.Close();
            }
            catch
            {
                return null;
            }

            return bEncrypt ? Convert.ToBase64String(memStream.ToArray()) : Encoding.Unicode.GetString(memStream.ToArray());
        }

        public static string Encrypt(string strData, string strPassword)
        {
            return Crypt(strData, strPassword, true);
        }

        public static string Decrypt(string strData, string strPassword)
        {
            return Crypt(strData, strPassword, false);
        }
	}
}