using System;
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
        /// musicTitle:上传歌曲的名称 (需要使用BASE64传递 , 避免HEADER编码问题无法传输)
        /// 
        /// Multipart承载所有上传的文件   文件大小要求小于40MB
        /// </summary>
        /// <returns>具体查询ResultMsg内的消息</returns>
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
            catch (Exception)
            {
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
            //设置上传目录
            var uploadFolderPath = HttpContext.Current.Request.PhysicalApplicationPath + "\\UploadedFiles\\" + username + "\\";
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
                            //项目的类别名称
                            var argTpyeName = item.Headers.ContentDisposition.Name.Replace("\"", "");
                            //临时目录路径对象
                            var path = uploadFolderPath;
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

        /// <summary>
        /// 获取上传文件列表
        /// 
        /// GET方式
        /// 参数通过HEAD传递
        /// 
        /// token:可用的Token
        /// </summary>
        /// <returns>具体查询ResultMsg内的消息</returns>
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
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false, "获取参数失败", null));
                throw;
            }
            //储存目录文件夹
            string BASE_UPLOAD_PATH = HttpContext.Current.Request.PhysicalApplicationPath+ "\\UploadedFiles\\"+username+"\\";
            string musicFolder = BASE_UPLOAD_PATH + "Music\\";
            string coverFolder = BASE_UPLOAD_PATH + "Cover\\";
            if (Directory.Exists(musicFolder) && Directory.GetFiles(musicFolder).Length > 0)
            {
                //当音频目录存在并且内部有文件存在
                var filesPath = Directory.GetFiles(musicFolder);
                var fileList = new List<MusicFile>();
                foreach (var path in filesPath)
                {
                    //歌曲信息储存对象
                    var responseObject = new MusicFile();
                    var file = new FileInfo(path);
                    //歌曲名称(文件名除去后缀)
                    var fileName = file.Name.Remove(file.Name.LastIndexOf("."));
                    //设置文件名
                    responseObject.fileName = file.Name;
                    //设置歌曲名称
                    responseObject.name = fileName;
                    //设置歌曲所属者
                    responseObject.ownerName = username;
                    //设置歌曲访问地址
                    responseObject.musicURL = "http://"+HttpContext.Current.Request.Url.Host+Data.SERVICE_PORT+"/UploadedFiles/"+username+"/Music/"+file.Name;
                    if(new FileInfo(path).Exists)
                    {
                        //如果歌曲有上传的封面 , 则设置封面访问地址
                        responseObject.coverURL = "http://" + HttpContext.Current.Request.Url.Host + Data.SERVICE_PORT + "/UploadedFiles/" + username + "/Cover/" + fileName+".jpg";
                    }
                    //将歌曲信息添加入返回列表信息中
                    fileList.Add(responseObject);
                }
                return Content(HttpStatusCode.OK, new ResultMsg<List<MusicFile>>(true, "获取上传文件列表成功", fileList));
            }
            else
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false, "您没有上传歌曲", null));
            }
        }

        /// <summary>
        /// 删除储存在云端的文件
        /// 
        /// GET方式
        /// 参数通过HEAD传递
        /// 
        /// token:可用的Token
        /// fileName:要删除的文件名 (需要使用BASE64传递 , 避免HEADER编码问题无法传输)
        /// </summary>
        /// <returns>具体查询ResultMsg内的消息</returns>
        [Route("RemoveFile")]
        public IHttpActionResult removeUploadedFiles()
        {
            string token = "";
            string fileName = "";
            string username = "";
            try
            {
                token = Request.Headers.GetValues("token").ToArray()[0].ToString();
                username = MySQLInterface.get().searchDB("token", "token", token)[0][0];
                fileName = TextUnits.decodeBase64(Request.Headers.GetValues("fileName").ToArray()[0].ToString());
                if (TextUnits.isTextEmpty(token) || TextUnits.isTextEmpty(fileName))
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false, "参数缺失", null));
                throw;
            }
            //储存目录文件夹
            string BASE_UPLOAD_PATH = HttpContext.Current.Request.PhysicalApplicationPath + "\\UploadedFiles\\" + username + "\\";
            string musicFolder = BASE_UPLOAD_PATH + "Music\\";
            string coverFolder = BASE_UPLOAD_PATH + "Cover\\";

            FileInfo musicFile = new FileInfo(musicFolder + fileName);
            FileInfo coverFile = new FileInfo(coverFolder + fileName.Remove(fileName.LastIndexOf(".")) + ".jpg");

            if (musicFile.Exists)
            {
                //文件存在
                musicFile.Delete();
                if (coverFile.Exists)
                {
                    coverFile.Delete();
                }
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(true, "移除云端文件成功", null));
            }
            else
            {
                //文件不存在
                return Content(HttpStatusCode.OK, new ResultMsg<Object>(false, "云端不存在此文件", null));
            }        
        }
    }
}
