using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyFinanceBackend.Utils
{
    internal static class PasswordUtils
    {
        private const string C_KEY = "myFinanceCKeyAbc";
        private const string C_IVK = "myFinanceCivAbCd";

        public static string EncryptText(string openText, string customCKey = C_KEY, string customCIvk = C_IVK)
        {
            var rc2Csp = new RC2CryptoServiceProvider();
            var encryptor = rc2Csp.CreateEncryptor(Convert.FromBase64String(customCKey), Convert.FromBase64String(customCIvk));
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    var toEncrypt = Encoding.Unicode.GetBytes(openText);

                    csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
                    csEncrypt.FlushFinalBlock();

                    var encrypted = msEncrypt.ToArray();

                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        public static string DecryptText(string encryptedText, string customCKey = C_KEY, string customCIvk = C_IVK)
        {
            var rc2Csp = new RC2CryptoServiceProvider();
            var decryptor = rc2Csp.CreateDecryptor(Convert.FromBase64String(customCKey), Convert.FromBase64String(customCIvk));
            using (var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText)))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    var bytes = new List<byte>();
                    int b;
                    do
                    {
                        b = csDecrypt.ReadByte();
                        if (b != -1)
                        {
                            bytes.Add(Convert.ToByte(b));
                        }

                    }
                    while (b != -1);

                    return Encoding.Unicode.GetString(bytes.ToArray());
                }
            }
        }
    }
}
