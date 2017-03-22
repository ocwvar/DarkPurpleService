using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DarkPurpleService.Units
{
    public class TokenUnits
    {
        /// <summary>
        /// 生成Token
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>可以使用的Token</returns>
        public string getToken(string username)
        {
            Random random = new Random();
            return username + random.Next().ToString();
        }

        /// <summary>
        /// 检查Token是否可用
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Token是否可用</returns>
        /// 
        //private static int timeOutLimit = 3600; //一小时
        //private static int timeOutLimit = 60; //测试 , 一分钟
        public bool checkToken(string token)
        {
            List<string[]> result = MySQLInterface.get().searchDB("token", "token", token);
            return result != null && result[0][2] == token;
        }
    }
}