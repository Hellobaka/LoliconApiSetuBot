using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using System.IO;
using System.Net;

namespace me.cqp.luohuaming.Setu.Code
{
    #region --json解析类--
    //Lolicon Api 解析
    public class Data
    {
        /// <summary>
        /// 作品 PID
        /// </summary>
        public string pid { get; set; }
        /// <summary>
        /// 作品所在 P
        /// </summary>
        public int p { get; set; }
        /// <summary>
        ///作者 UID
        ///</summary>
        public string uid { get; set; }
        /// <summary>
        ///作品标题
        ///</summary>
        public string title { get; set; }
        /// <summary>
        ///作者名（入库时，并过滤掉 @ 及其后内容）
        ///</summary>
        public string author { get; set; }
        /// <summary>
        ///图片链接（可能存在有些作品因修改或删除而导致 404 的情况）
        ///</summary>
        public string url { get; set; }
        /// <summary>
        ///是否 R18（在色图库中的分类，并非作者标识的 R18）
        ///</summary>
        public string r18 { get; set; }
        /// <summary>
        ///原图宽度 px
        ///</summary>
        public string width { get; set; }
        /// <summary>
        ///原图高度 px
        ///</summary>
        public string height { get; set; }
        /// <summary>
        ///作品标签，包含标签的中文翻译（有的话）
        ///</summary>
        public List<string> tags { get; set; }

        public IList<string> ext_urls { get; set; }
        public int da_id { get; set; }
        public string author_name { get; set; }
        public string author_url { get; set; }
        public int? pixiv_id { get; set; }
        public string member_name { get; set; }
        public int? member_id { get; set; }
    }
    public class Setu
    {
        public int code { get; set; } //返回码，可能值详见后续部分
        public string msg { get; set; } //错误信息之类的
        public int quota { get; set; } //剩余调用额度
        public int quota_min_ttl { get; set; } //距离下一次调用额度恢复(+1)的秒数
        public int count { get; set; } //结果数
        public List<Data> data { get; set; } //色图数组
    }
    //code
    //值		说明
    //-1		内部错误，请向 i@loli.best 反馈
    //0		成功
    //401		APIKEY 不存在或被封禁
    //403	由于不规范的操作而被拒绝调用
    //404	找不到符合关键字的色图
    //429	达到调用额度限制
    #endregion

    #region --api接口说明--
    //api地址:https://api.lolicon.app/setu
    //可用参数:
    //apikey=872375555e8585a40317f0
    //r18=0 //0为非 R18，1为 R18，2为混合
    //keyword= //若指定关键字，将会返回从插画标题、作者、标签中模糊搜索的结果
    //num=1 //一次返回的结果数量，范围为1到10，不提供 APIKEY 时固定为1；在指定关键字的情况下，结果数量可能会不足指定的数量
    //proxy=i.pixiv.cat //设置返回的原图链接的域名，你也可以设置为disable来得到真正的原图链接
    //size1200=false //是否使用 master_1200 缩略图，即长或宽最大为 1200px 的缩略图，以节省流量或提升加载速度（某些原图的大小可以达到十几MB）
    //使用 APIKEY 可获得300次/天的调用额度，接口允许不使用 APIKEY，但调用额度很低，并且具有一定限制，仅供测试使用

    //SauceNao接口:https://saucenao.com/search.php
    //可用参数:
    //api_key=56faa0cddf50860330a295e0c331be7c4b4c021f //<your api key>
    //db=999 //<index num or 999 for all database>
    //output_type=2 //0=normal htm,1=xml api(not implemented),2=json api
    //testmode=1
    //numres=16 //返回的个数
    //url=http://xxx.xxx.xxx/xxx.jpg //提交的图片网址
    #endregion

    public class GetSetu
    {
        string api = "https://api.lolicon.app/setu?";
        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="ordertext">除指令外的控制文本</param>
        /// <returns></returns>
        public List<string> GetSetuPic(string ordertext)
        {
            //定义
            //List数组内 第一个元素为信息 第二个元素为图片路径
            //信息正常返回json，出现异常返回数字
            //401：接口调用次数达到上限
            //402：图片下载失败
            //403：其他错误（超时，404等）详情写入日志
            try
            {
                WebProxy proxy = null;
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini");
                ini.Load();
                if (ini.Object["Proxy"]["IsEnabled"].GetValueOrDefault("0") == "1")
                {
                    try
                    {
                        //代理设置
                        string uri, username, pwd;
                        uri = ini.Object["Proxy"]["ProxyUri"].GetValueOrDefault("0");
                        username = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("0");
                        pwd = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("0");

                        proxy = new WebProxy();
                        proxy.Address = new Uri(uri);
                        proxy.Credentials = new NetworkCredential(username, pwd);
                    }
                    catch (Exception ex)
                    {
                        CQSave.cqlog.Info("Proxy错误", $"设置的代理无效，信息:{ex.Message}");
                    }
                }
                Event_GroupMessage.revoke = false;
                List<string> result = new List<string>();
                string url = string.Empty;
                //拼接Url
                if (ini.Object["Config"]["ApiSwitch"].GetValueOrDefault("0") == "1")
                {
                    string apikey = ini.Object["Config"]["ApiKey"].GetValueOrDefault("0");
                    url = api + $"apikey={apikey}&";
                }
                else
                {
                    url = api;
                }
                url += GetOrderText(ordertext);
                if (url.Contains("r18=1")&&ini.Object["R18"]["R18PicRevoke"]=="1")
                {
                    Event_GroupMessage.revoke = true;//用于后续撤回
                }
                string json = "";
                CQSave.cqlog.Debug("debug", url);
                try
                {
                    //访问接口
                    byte[] by = Get(url, 10000, proxy);
                    json = Encoding.UTF8.GetString(by);
                    if (string.IsNullOrEmpty(json))
                    {
                        throw new NullReferenceException();
                    }
                }
                catch (Exception e)
                {
                    CQSave.cqlog.Info("Error", e.Message + " ");
                    result.Add("403" + e.Message);
                    result.Add(@"\LoliconPic\error.jpg");
                    return result;
                }
                CQSave.cqlog.Debug("debug", json + " ");
                //检查路径是否存在
                if (!Directory.Exists(CQSave.ImageDirectory + @"\LoliconPic\"))
                {
                    Directory.CreateDirectory(CQSave.ImageDirectory + @"\LoliconPic\");
                }
                //反序列化json
                Setu deserialize = JsonConvert.DeserializeObject<Setu>(json);
                if (deserialize.code != 0)//非成功调用
                {
                    result.Add(json);
                    result.Add(@"\LoliconPic\error.jpg");
                    return result;
                }
                //获取Data数组信息
                List<Data> pic = deserialize.data;
                foreach (var item in pic)
                {
                    try
                    {
                        HttpWebClient http = new HttpWebClient
                        {
                            TimeOut = 10000,//超时时间10s
                            Proxy = proxy
                        };
                        string path = CQSave.ImageDirectory + @"\LoliconPic\" + item.pid + ".jpg";
                        if (!File.Exists(path))
                        {
                            http.DownloadFile(item.url, path);
                            AntiHX(CQSave.ImageDirectory + @"\LoliconPic\" + item.pid + ".jpg");
                        }
                        result.Add(json);
                        result.Add(@"\LoliconPic\" + item.pid + ".jpg");
                        http.Dispose();
                    }
                    catch (Exception e)
                    {
                        CQSave.cqlog.Info("Error", "在" + e.Source + "上, 发送错误: " + e.Message + " 有" + e.StackTrace);
                        result.Add("402" + e.Message);
                        result.Add(@"\LoliconPic\error.jpg");
                        return result;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                CQSave.cqlog.Info("Error", "在" + e.Source + "上, 发送错误: " + e.Message + " 有" + e.StackTrace);
                List<string> throww = new List<string>
                {
                    "403"+e.Message,
                    @"\LoliconPic\error.jpg"
                };
                return throww;
            }
        }

        /// <summary>
        /// 向服务器发送 HTTP GET 请求
        /// </summary>
        /// <param name="url">完整的网页地址
        ///		<para>必须包含 "http://" 或 "https://"</para>
        /// </param>
        /// <param name="timeout">超时时间</param>
        /// <param name="proxy">代理 <see cref="HttpWebClient"/> 的 <see cref="WebProxy"/></param>
        /// <returns></returns>
        public static byte[] Get(string url, int timeout, WebProxy proxy)
        {
            HttpWebClient httpWebClient = new HttpWebClient();
            httpWebClient.TimeOut = timeout;
            httpWebClient.Proxy = proxy;
            httpWebClient.AllowAutoRedirect = true;
            httpWebClient.AutoCookieMerge = true;
            byte[] result = httpWebClient.DownloadData(new Uri(url));
            return result;
        }
        /// <summary>
        /// 改变图片的MD5来尝试反和谐
        /// </summary>
        /// <param name="img">原图</param>
        /// <returns></returns>
        public static void AntiHX(string path)
        {
            Image img = Image.FromFile(path);
            Bitmap bitMap = new Bitmap(img.Width, img.Height);
            Graphics g1 = Graphics.FromImage(bitMap);

            Color pixelColor = bitMap.GetPixel(0, 0);
            Color targetcolor = ChangeColor(pixelColor);
            Pen pen = new Pen(targetcolor);
            Rectangle rect = new Rectangle(0, 0, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(img.Width - 1, 0);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(img.Width - 1, 0, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(0, img.Height - 1);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(0, img.Height - 1, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(img.Width - 1, img.Height - 1);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(img.Width - 1, img.Height - 1, 1, 1);
            g1.DrawRectangle(pen, rect);

            g1.Dispose();
            img.Save(path + ".tmp");
            img.Dispose();
            File.Delete(path);
            File.Move(path + ".tmp", path);
        }
        /// <summary>
        /// 颜色轻微变更
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        static Color ChangeColor(Color col)
        {
            byte red, blue, green;
            if (col.R == 0 || col.R == 255)
            {
                red = (col.R == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                red = Convert.ToByte(col.R + 1);
            }
            if (col.G == 0 || col.G == 255)
            {
                green = (col.G == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                green = Convert.ToByte(col.G + 1);
            }
            if (col.B == 0 || col.B == 255)
            {
                blue = (col.B == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                blue = Convert.ToByte(col.R + 1);
            }
            Color result = Color.FromArgb(red, green, blue);
            return result;
        }

        static string GetOrderText(string ordertext)
        {
            ordertext = ordertext.ToLower().Replace(" ", "");
            if (string.IsNullOrEmpty(ordertext)) return string.Empty;
            int r18 = 0;
            string keyword = string.Empty;
            if (ordertext.Contains("r18"))
            {
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini");
                ini.Load();
                if (ini.Object["R18"]["Enabled"].GetValueOrDefault("0") == "1")
                    r18 = 1;
                else
                    CQSave.cqlog.Warning("R18开关", "R18开关处于关闭状态，若想调用，请打开扩展设置中的选项");
            }
            keyword = ordertext.Replace("r18", "");
            return $"r18={r18}&keyword={keyword}";
        }
    }
}
