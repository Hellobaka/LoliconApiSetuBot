using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class GetLoliconPic : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.LoliConPic))
            {
                PublicVariables.LoliConPic = Guid.NewGuid().ToString();
            }
            return PublicVariables.LoliConPic;
        }

        public bool Judge(string destStr)
        {
            return destStr.Replace("＃", "#").StartsWith(GetOrderStr());
        }
        public static bool RevokeState { get; set; } = false;
        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult()
            {
                Result = true,
                SendFlag = false,
            };
            //检查额度限制
            if (QuotaHelper.QuotaCheck(e.FromGroup, e.FromQQ) is false)
            {
                return result;
            }
            //拉取图片，处理时间受地区与网速限制
            var pic = GetLoliConPicHelper
                .GetSetuPic(e.Message.Text.Substring(GetOrderStr().Length), out string objectTostring);
            pic.SendID = e.FromGroup.Id;
            try
            {
                SendText text = new SendText
                {
                    MsgToSend = new List<string>(),
                    SendID = e.FromGroup,
                };
                objectTostring.Replace("<@>", e.FromQQ.CQCode_At().ToSendString());
                // 处理返回文本，替换可配置文本为结果，发送处理结果                
                if (!string.IsNullOrWhiteSpace(objectTostring))
                    text.MsgToSend.Add(objectTostring);
                result.SendObject.Add(text);
                result.SendFlag = true;
                IniConfig ini = MainSave.ConfigMain;
                if (pic.HandlingFlag)//是否成功处理消息
                {
                    //TODO: Improve this code
                    //处理一番后发现, 成功发送时只有一个图片文本, 虽说不规范, 以后再说吧
                    var msg = e.FromGroup.SendGroupMessage(pic.MsgToSend[0]);
                    if (PublicVariables.R18_PicRevoke)//自动撤回
                    {
                        Task task = new Task(() =>
                        {
                            Thread.Sleep(PublicVariables.R18_RevokeTime * 1000);
                            e.CQApi.RemoveMessage(msg.Id);
                        }); task.Start();
                    }
                }
                else//处理消息失败
                {
                    result.SendFlag = true;
                    result.SendObject.Add(pic);
                    QuotaHelper.PlusMemberQuota(e.FromGroup.Id, e.FromQQ.Id);
                }
            }
            catch (Exception exc)
            {
                SendText text = new SendText
                {
                    MsgToSend = new List<string>(),
                    SendID = e.FromGroup,
                };
                text.MsgToSend.Add($"发生未知错误,错误信息:在{exc.Source}上, 发生错误: {exc.Message}");
                result.SendObject.Add(text);
                result.SendFlag = true;
            }
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

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
        public override string ToString()
        {
            //<code> 返回码
            //<msg> 错误信息之类的
            //<quota> 剩余调用额度
            //<quota_min_ttl> 距离下一次调用额度恢复(+1)的秒数
            //<quota_time> 下次额度回复的DataTime表示
            //<pid> 作品 PID
            //<p> 作品所在 P
            //<uid> 作者 UID
            //<title> 作品标题
            //<author> 作者名（入库时，并过滤掉 @ 及其后内容）
            //<url> 图片链接（可能存在有些作品因修改或删除而导致 404 的情况）
            //<r18> 是否 R18（在色图库中的分类，并非作者标识的 R18）
            //<width> 原图宽度 px
            //<height> 原图高度 px
            //<#> 自定义变量
            try
            {
                //计算额度恢复时间
                DateTime dt = DateTime.Now.AddSeconds(quota_min_ttl);
                switch (code)
                {
                    case 429:
                        PublicVariables.OutofQuota = PublicVariables.OutofQuota.Replace("<quota_time>", (DateTime.Now.Hour <= dt.Hour) ? $"{dt:HH:mm}" : $"明天 {dt:HH:mm}");
                        return PublicVariables.OutofQuota;
                    case 404:
                        return PublicVariables.PicNotFound;
                    case 401:
                        return "APIKEY无效，请更换";
                    case 0:
                        string result = PublicVariables.Sucess.Replace("<code>", code.ToString());
                        result = result.Replace("<msg>", msg);
                        result = result.Replace("<quota>", quota.ToString());
                        result = result.Replace("<quota_min_ttl>", quota_min_ttl.ToString());
                        result = result.Replace("<quota_time>", (DateTime.Now.Hour <= dt.Hour) ? $"{dt:HH:mm}" : $"明天 {dt:HH:mm}");
                        List<Data> pic = data;
                        Data picinfo = pic[0];
                        result = result.Replace("<pid>", picinfo.pid);
                        result = result.Replace("<p>", picinfo.p.ToString());
                        result = result.Replace("<uid>", picinfo.uid);
                        result = result.Replace("<title>", picinfo.title);
                        result = result.Replace("<author>", picinfo.author);
                        result = result.Replace("<url>", picinfo.url);
                        result = result.Replace("<r18>", picinfo.r18);
                        result = result.Replace("<width> ", picinfo.width);
                        result = result.Replace("<height>", picinfo.height);
                        return result;
                    default:
                        return "默认返回";
                }
            }
            catch (Exception e)
            {
                MainSave.CQLog.Info("解析错误", $"错误信息:{e.Message}\n{e.StackTrace}");
                return "解析错误，无法继续";
            }
        }
    }
    //code
    //值		说明
    //-1		内部错误，请向 i@loli.best 反馈
    //0		成功
    //401		APIKEY 不存在或被封禁
    //403	由于不规范的操作而被拒绝调用
    //404	找不到符合关键字的色图
    //429	达到调用额度限制

    public class SetuV2
    {
        public string error { get; set; }
        public Datum[] data { get; set; }
        public override string ToString()
        {
            string result = PublicVariables.Sucess.Replace("<msg>", error);
            Datum picinfo = data[0];
            result = result.Replace("<pid>", picinfo.pid.ToString());
            result = result.Replace("<p>", picinfo.p.ToString());
            result = result.Replace("<uid>", picinfo.uid.ToString());
            result = result.Replace("<title>", picinfo.title);
            result = result.Replace("<author>", picinfo.author);
            result = result.Replace("<url>", picinfo.urls.original);
            result = result.Replace("<r18>", picinfo.r18.ToString());
            result = result.Replace("<width> ", picinfo.width.ToString());
            result = result.Replace("<height>", picinfo.height.ToString());
            return result;
        }
    }

    public class Datum
    {
        public int pid { get; set; }
        public int p { get; set; }
        public int uid { get; set; }
        public string title { get; set; }
        public string author { get; set; }
        public bool r18 { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string[] tags { get; set; }
        public string ext { get; set; }
        public long uploadDate { get; set; }
        public Urls urls { get; set; }
    }

    public class Urls
    {
        public string original { get; set; }
    }

    #endregion

    public static class GetLoliConPicHelper
    {
        private static readonly string api = "https://api.lolicon.app/setu?";
        private static readonly string apiV2 = "https://api.lolicon.app/setu/v2?";
        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="ordertext">除指令外的控制文本</param>
        /// <returns></returns>
        public static SendText GetSetuPic(string ordertext, out string objectTostring)
        {
            SendText result = new SendText();
            objectTostring = string.Empty;
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                try
                {
                    IniConfig ini = MainSave.ConfigMain;
                    string url = apiV2;
                    //拼接Url
                    if (PublicVariables.Lolicon_ApiSwitch)
                    {
                        url = apiV2 + $"apikey={PublicVariables.Lolicon_ApiKey}";
                    }
                    url += GetOrderText(ordertext);

                    if (url.Contains("r18=1") && ini.Object["R18"]["R18PicRevoke"] == "1")
                    {
                        GetLoliconPic.RevokeState = true;//用于后续撤回
                    }
                    string json = "";
                    try
                    {
                        json = http.DownloadString(url);
                    }
                    catch (Exception e)
                    {
                        MainSave.CQLog.Info("Error", e.Message);
                        result.MsgToSend.Add(e.Message);
                        result.HandlingFlag = false;
                        return result;
                    }
                    //检查路径是否存在
                    if (!Directory.Exists(MainSave.ImageDirectory + @"\LoliconPic\"))
                    {
                        Directory.CreateDirectory(MainSave.ImageDirectory + @"\LoliconPic\");
                    }
                    //反序列化json
                    SetuV2 deserialize = JsonConvert.DeserializeObject<SetuV2>(json);
                    if (deserialize.data.Length == 0)//非成功调用
                    {
                        result.MsgToSend.Add("哦淦 老兄你的xp好机八小众啊 找不到啊");
                        result.HandlingFlag = true;
                        return result;
                    }
                    objectTostring = deserialize.ToString();
                    var pic = deserialize.data[0];
                    string path = Path.Combine(MainSave.ImageDirectory, "LoliconPic", $"{pic.pid}.jpg");
                    if (!File.Exists(path))
                    {
                        http.CookieCollection = new CookieCollection();
                        http.DownloadFile($"https://pixiv.re/{pic.pid}.jpg", path);
                        CommonHelper.AntiHX(path);
                    }
                    result.MsgToSend.Add(CQApi.CQCode_Image(@"\LoliconPic\" + pic.pid + ".jpg").ToSendString());
                    return result;
                }
                catch (Exception e)
                {
                    MainSave.CQLog.Info("Error", $"发生错误的对象{e.Source} , 发送错误: {e.Message}\n{e.StackTrace}");
                    result.MsgToSend.Add(e.Message);
                    result.HandlingFlag = false;
                    return result;
                }
            }
        }
        static string GetOrderText(string ordertext)
        {
            ordertext = ordertext.ToLower().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(ordertext))
                return string.Empty;
            int r18 = 0;
            if (ordertext.Contains("r18"))
            {
                if (PublicVariables.R18_Flag)
                    r18 = 1;
                else
                    MainSave.CQLog.Warning("R18开关", "R18开关处于关闭状态，若想调用，请打开扩展设置中的选项");
            }
            string keyword = ordertext.Replace("r18", "");
            return $"&r18={r18}&keyword={keyword}";
        }
    }
}
