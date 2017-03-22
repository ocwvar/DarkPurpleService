using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DarkPurpleService.Units;
using DarkPurpleService.Models;


namespace DarkPurpleService.Controllers
{
    
    [RoutePrefix("api/User")]
    public class UserController : ApiController
    {

        private TokenUnits tokenUnits;

        public UserController()
        {
            this.tokenUnits = new TokenUnits();
        }

        /// <summary>
        /// 登录
        /// 
        /// POST方式
        /// 参数通过HEAD传递
        /// 
        /// username:用户名
        /// password:密码
        /// </summary>
        /// <returns>200成功: 返回可用的Token  403错误:对应的错误消息</returns>
        [Route("Login")]
        public IHttpActionResult login()
        {
            string username = "";
            string password = "";
            try
            {
                username = Request.Headers.GetValues("username").ToArray()[0].ToString();
                password = Request.Headers.GetValues("password").ToArray()[0].ToString();
            }
            catch (Exception)
            {
                //当从请求头中提取参数的时候 , 如果参数缺失会抛出异常
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"参数缺失",null));
                throw;
            }
            string resultToken = getToken(username, password);
            if (resultToken == null)
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"登录失败",null));
            }
            else
            {
                return Content(HttpStatusCode.OK, new ResultMsg<String>(true, "成功", resultToken));
            }
        }

        /// <summary>
        /// 注册账户
        /// 
        /// POST方式
        /// 参数通过HEAD传递
        /// 
        /// username:用户名
        /// password:密码
        /// </summary>
        /// <returns>200成功: 返回可用的Token  403错误:对应的错误消息</returns>
        [Route("Register")]
        public IHttpActionResult register()
        {
            string username = "";
            string password = "";
            try
            {
                username = Request.Headers.GetValues("username").ToArray()[0].ToString();
                password = Request.Headers.GetValues("password").ToArray()[0].ToString();
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"参数缺失",null));
                throw;
            }
            if (TextUnits.isTextEmpty(username) || TextUnits.isTextEmpty(password))
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"注册请求的账密字符串无效",null));
            }
            else
            {
                //检查是否有相同名字的账户
                if(MySQLInterface.get().searchDB("user","username",username) != null)
                {
                    //有相同的用户 , 无法注册
                    return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"存在相同的用户名",null));
                }
                else
                {
                    //往数据库中写入新的用户
                    if(MySQLInterface.get().putValue("user", new string[] { "username", "password" }, new string[] { username, password }))
                    {
                        //生成token
                        string token = getToken(username,password);
                        if(token != null)
                        {
                           //如果生成成功 , 则返回成功信息并携带Token
                            return Content(HttpStatusCode.OK, new ResultMsg<String>(true, "成功", token));
                        }
                        else
                        {
                            return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"无法生成Token , 请登录重试",null));
                        }
                    }
                    else
                    {
                        return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"数据库无法写入",null));
                    }
                }
            }
        }

        /// <summary>
        /// 验证账密是否正确 , 并且返回用以使用的Token数据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>验证使用的Token , 无法验证账密则返回NULL</returns>
        private string getToken(string username,string password)
        {
            if(TextUnits.isTextEmpty(username) || TextUnits.isTextEmpty(password))
            {
                //账密文本为空
                return null;
            }
            else
            {
                List<String[]> result = MySQLInterface.get().searchDB("user", "username", username);
                if(result == null)
                {
                    //没有查询到对应的账户
                    return null;
                }
                else
                {
                    string[] userInfoSet = result[0];
                    //检查账密是否匹配
                    if(userInfoSet[userInfoSet.Length-1] == password)
                    {
                        //获取账户对应的token
                        string token = tokenUnits.getToken(username);

                        //计算当前的时间戳
                        DateTime dateStart = new DateTime(1970, 1, 1, 0, 0, 0);
                        TimeSpan timeSpan = DateTime.UtcNow - dateStart;
                        int createTimeSeconds = (int)timeSpan.TotalSeconds;

                        //先删除旧的token数据
                        MySQLInterface.get().removeRow("token", "username", username);
                        if (MySQLInterface.get().putValue("token", new string[] { "username", "token","create_time" }, new string[] { username, token, createTimeSeconds .ToString()}))
                        {
                            //如果Token写入数据库成功
                            return token;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        //账密验证失败
                        return null;
                    }
                }
            }
        }

    }
}
