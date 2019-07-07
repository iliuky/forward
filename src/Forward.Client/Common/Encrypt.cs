using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Forward.Client.Common
{
    public class Encrypt
    {
        /// <summary>
        /// MD5 加密
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public static string MD5(string content)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                return Convert.ToBase64String(md5.ComputeHash(bytes));
            }
        }
    }
}
