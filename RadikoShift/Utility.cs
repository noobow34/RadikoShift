using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RadikoShift
{
    public static class Utility
    {
        public static class Text
        {
            public static DateTime StringToDate(string src)
            {
                src = Regex.Replace(src, "[^0-9]", "");
                if (src.Length < 14)
                {
                    return DateTime.MinValue;
                }

                DateTime res;
                int year = int.TryParse(src.Substring(0, 4), out year) ? year : 0;
                int month = int.TryParse(src.Substring(4, 2), out month) ? month : 0;
                int day = int.TryParse(src.Substring(6, 2), out day) ? day : 0;
                int hour = int.TryParse(src.Substring(8, 2), out hour) ? hour : 0;
                int min = int.TryParse(src.Substring(10, 2), out min) ? min : 0;
                int sec = int.TryParse(src.Substring(12, 2), out sec) ? sec : 0;

                if (year == 0)
                {
                    return DateTime.MinValue;
                }

                res = new DateTime(year, month, day, hour, min, sec, new GregorianCalendar());
                return res;
            }
            
            /// <summary>
            /// sha256作成
            /// </summary>
            /// <param name="src"></param>
            /// <returns></returns>
            public static string Sha256(string src)
            {
                var res = "";

                using (var sha256 = SHA256.Create())
                {
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(src));
                    res = BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }

                return res;
            }
            
            /// <summary>
            /// 
            /// </summary>
            /// <param name="src"></param>
            /// <param name="encKey"></param>
            /// <returns></returns>
            public static string Encrypt(string src, string encKey)
            {
                if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(encKey)) return "";
                try
                {
                    using var aes = Aes.Create();
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.IV = Encoding.UTF8.GetBytes(encKey.Substring(0, 16));
                    aes.Key = Encoding.UTF8.GetBytes(encKey);
                    aes.Padding = PaddingMode.PKCS7;

                    var srcByte = Encoding.UTF8.GetBytes(src);
                    var encrypt = aes.CreateEncryptor().TransformFinalBlock(srcByte, 0, srcByte.Length);
                    return Convert.ToBase64String(encrypt);
                }
                catch
                {
                    return "";
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="src"></param>
            /// <param name="encKey"></param>
            /// <returns></returns>
            public static string Decrypt(string src, string encKey)
            {
                if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(encKey)) return "";

                try
                {
                    using var aes = Aes.Create();
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.IV = Encoding.UTF8.GetBytes(encKey.Substring(0, 16));
                    aes.Key = Encoding.UTF8.GetBytes(encKey);
                    aes.Padding = PaddingMode.PKCS7;

                    var srcByte = Convert.FromBase64String(src);
                    var decript = aes.CreateDecryptor().TransformFinalBlock(srcByte, 0, srcByte.Length);
                    return Encoding.UTF8.GetString(decript);
                }
                catch
                {
                    return "";
                }
            }

            /// <summary>
            /// ファイルサイズを文字列に
            /// </summary>
            /// <param name="length"></param>
            /// <returns></returns>
            public static string ToSizeString(long length)
            {
                if (length < 1000)
                {
                    return $"{length}B";
                }
                else if (length < 1000 * 1000)
                {
                    return $"{length / 1000}KB";
                }
                else if (length < 1000 * 1000 * 1000)
                {
                    return $"{length / 1000 / 1000}MB";
                }
                else
                {
                    return $"{length / 1000 / 1000 / 1000}GB";
                }
            }
        }
    }
}