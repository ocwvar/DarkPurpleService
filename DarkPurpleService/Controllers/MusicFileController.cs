﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using DarkPurpleService.Units;
using DarkPurpleService.Models;
using System.Threading.Tasks;
using System.IO;
using Id3Lib;

namespace DarkPurpleService.Controllers
{
    [RoutePrefix("api/Files")]
    public class MusicFileController : ApiController
    {

        private TokenUnits tokenUnits;

        public MusicFileController()
        {
            this.tokenUnits = new TokenUnits();
        }

        /// <summary>
        /// 上传文件
        /// 
        /// POST方式
        /// 参数通过HEAD传递
        /// 
        /// token:可用的Token
        /// fileType:上传文件的类型
        /// musicTitle:上传歌曲的名称
        /// 
        /// Multipart承载所有上传的文件   文件大小要求小于40MB
        /// </summary>
        /// <returns>200成功  403错误:对应的错误消息</returns>
        [Route("Upload")]
        public async Task<IHttpActionResult> upload()
        {
            string token = "";
            string fileType = "";
            string musicTitle = "";
            try
            {
                token = Request.Headers.GetValues("token").ToArray()[0].ToString();
                fileType = Request.Headers.GetValues("fileType").ToArray()[0].ToString();
                musicTitle = TextUnits.decodeBase64(Request.Headers.GetValues("musicTitle").ToArray()[0].ToString());
                musicTitle = musicTitle.Replace(":","").Replace("?", "").Replace("|", "").Replace("/", "").Replace("\"", "").Replace("\\", "").Replace("*", "").Replace("<", "").Replace(">", "");
                if (TextUnits.isTextEmpty(token) || TextUnits.isTextEmpty(fileType) || TextUnits.isTextEmpty(musicTitle))
                {
                    throw new Exception();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"参数缺失",null));
                throw;
            }
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                //如果不是Multipart类型的请求
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"无效请求类型",null));
            }
            if(TextUnits.isTextEmpty(token) || tokenUnits.checkToken(token))
            {
                //如果Token是无效的
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false,"无效Token",null));
            }

            //获取Multipart的内容
            MultipartMemoryStreamProvider provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

            //以用户名作为子目录分类
            var username = MySQLInterface.get().searchDB("token", "token", token)[0][0];

            foreach (var item in provider.Contents)
            {
                if(item.Headers.ContentDisposition.Name != null)
                {
                    //获取到文件的流
                    var stream = item.ReadAsStreamAsync().Result;
                    
                    if(stream.Length > 0)
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            var fileBytes = reader.ReadBytes((int)stream.Length);
                            //上传储存目录
                            var path = "D:\\UploadedFiles\\"+username+"\\";
                            //项目的类别名称
                            var argTpyeName = item.Headers.ContentDisposition.Name.Replace("\"", "");
                            //根据文件类型不同设置不同的子目录
                            if (argTpyeName == "cover")
                            {
                                path = path + "Cover\\";
                            }
                            else
                            {
                                path = path + "Music\\";
                            }
                            //如果目录不存在则创建目录
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            //设置后缀名
                            if(argTpyeName == "cover")
                            {
                                path = path + musicTitle + ".jpg";
                            }
                            else
                            {
                                path = path + musicTitle + "." + fileType;
                            }
                            //写入文件           
                            try
                            {
                                File.WriteAllBytes(path, fileBytes);
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine(e);
                                return Content(HttpStatusCode.OK, new ResultMsg<Object>(true, "文件无效", null));
                                throw;
                            }                  
                        }
                    }
                }
            }
            return Content(HttpStatusCode.OK, new ResultMsg<Object>(true, "上传文件成功", null));
        }

        [Route("MyFiles")]
        public IHttpActionResult getUploadedFiles()
        {
            string token = "";
            string username = "";
            try
            {
                token = Request.Headers.GetValues("token").ToArray()[0].ToString();
                username = MySQLInterface.get().searchDB("token", "token", token)[0][0];
                if (TextUnits.isTextEmpty(token) || TextUnits.isTextEmpty(username))
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false, "参数缺失", null));
                throw;
            }
            string folderPath = HttpContext.Current.Request.PhysicalApplicationPath;
            return Ok(folderPath);
        }    

    }
}