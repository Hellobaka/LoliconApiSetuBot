using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.API;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class LoliconPic : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(OrderConfig.LoliconPicOrder))
            {
                return Guid.NewGuid().ToString();
            }
            return OrderConfig.LoliconPicOrder;
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
                SendFlag = true,
            };
            SendText sendText = new SendText
            {
                SendID = e.FromGroup
            };
            result.SendObject.Add(sendText);
            if (QuotaHistory.GroupQuotaDict[e.FromGroup] >= AppConfig.MaxGroupQuota)
            {
                sendText.MsgToSend.Add(AppConfig.MaxGroupResoponse);
                return result;
            }

            if (QuotaHistory.QueryQuota(e.FromGroup, e.FromQQ) <= 0)
            {
                sendText.MsgToSend.Add(AppConfig.MaxMemberResoponse);
                return result;
            }
            try
            {
                int quota = AppConfig.MaxPersonQuota - QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, -1);
                e.FromGroup.SendGroupMessage(AppConfig.StartResponse.Replace("<count>", quota.ToString()));
                string basePath = Path.Combine(MainSave.ImageDirectory, "LoliconPic");
                Directory.CreateDirectory(basePath);
                string order = e.Message.Text.ToLower().Replace("＃", "#").Replace(GetOrderStr(), "").Replace(" ", "");
                bool r18 = order.Contains("r18");
                if(r18 && AppConfig.R18 is false)
                {
                    e.CQLog.Info("R18开关", "R18配置未开启，默认普通图片");
                    r18 = false;
                }

                order = order.Replace("r18", "");
                string url = $"https://api.lolicon.app/setu/v2?keyword={order}&r18={(r18 ? "1" : "0")}";
                using HttpWebClient http = new()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                };
                string json = http.DownloadString(url);
                SetuV2 deserialize = JsonConvert.DeserializeObject<SetuV2>(json);
                if (deserialize.data.Length == 0)
                {
                    sendText.MsgToSend.Add(AppConfig.PicNotFoundResoponse);
                    return result;
                }
                var pic = deserialize.data.First();
                string filename = new DirectoryInfo(basePath).GetFiles().FirstOrDefault(x => x.Name.Contains(pic.pid.ToString()))?.Name;
                e.FromGroup.SendGroupMessage(deserialize.ToString().Replace("<@>", e.FromQQ.CQCode_At().ToString()));
                if (string.IsNullOrEmpty(filename))// 本地无图片
                {
                    var fileinfo = new FileInfo(PixivAPI.DownloadPic(pic.urls.original, basePath));
                    filename = fileinfo.Name;
                }
                if(AppConfig.AntiBan)
                {
                    AntiBan(filename, out filename);                    
                }
                var msgItem = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(@"LoliconPic\" + filename));
                if (AppConfig.R18_PicRevoke && r18)
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(AppConfig.R18_RevokeTime);
                        e.CQApi.RemoveMessage(msgItem.Id);
                    }).Start();
                }
            }
            catch (WebException)
            {
                sendText.MsgToSend.Add($"网络错误，请重试");
                QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, 1);
            }
            catch (Exception exc)
            {
                e.CQLog.Info("lolicon", exc.Message + exc.StackTrace);
                sendText.MsgToSend.Add($"发生未知错误，错误信息: 在{exc.Source}上，发生错误: {exc.Message}");
                QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, 1);
            }
            return result;
        }

        private void AntiBan(string file, out string filename)
        {
            string path = Path.Combine(MainSave.ImageDirectory, "LoliconPic", file);
            FileInfo info = new(path);
            filename = info.Name;
            if(File.Exists(path) is false)
            {
                return;
            }
            using Bitmap img = (Bitmap)Image.FromFile(path);
            switch (AppConfig.AntiBanType)
            {
                case AntiBanType.Filp:
                    img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
                case AntiBanType.MD5:
                    Random rd = new();
                    img.SetPixel(0, 0, Color.FromArgb(rd.Next(0, 255), rd.Next(0, 255), rd.Next(0, 255)));
                    break;
                default:
                    break;
            }
            img.Save(info.FullName.Replace(info.Extension, "_anti" + info.Extension));
            filename = filename.Replace(info.Extension, "_anti" + info.Extension);
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            return new FunctionResult
            {
                Result = false
            };
        }
    }

    #region --json解析类--
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
            string result = AppConfig.SuccessResponse.Replace("<msg>", error);
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
}
