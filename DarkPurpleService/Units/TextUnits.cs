using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DarkPurpleService.Units
{
    public class TextUnits
    {
        /// <summary>
        /// 检查字符串是否为空
        /// </summary>
        /// <param name="text">要检查的字符串</param>
        /// <returns>True: 字符串为空   False:字符串不为空</returns>
        public static bool isTextEmpty(string text)
        {
            return text == null || text.Length <= 0;
        }

        /// <summary>
        /// BASE64解码
        /// </summary>
        /// <param name="encodedBase64">BASE64加密后的字符串</param>
        /// <returns>解密后的字符串,解密失败返回NULL</returns>
        public static string decodeBase64(string encodedBase64)
        {
            if (TextUnits.isTextEmpty(encodedBase64))
            {
                return null;
            }

            if (encodedBase64.Length % 4 != 0)
            {
                int shouldBeLength = ((encodedBase64.Length / 4) + 1) * 4;
                string endString = "";
                for (int i = 0; i < shouldBeLength - encodedBase64.Length; i++)
                {
                    endString = endString + "=";
                }
                encodedBase64 = encodedBase64 + endString;
            }

            return System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(encodedBase64));
        }
    }
}