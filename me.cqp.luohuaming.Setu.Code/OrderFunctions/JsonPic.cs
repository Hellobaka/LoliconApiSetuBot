using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.Model;
using System.Net;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class JsonPic : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        private static List<JsonToDeserize> JsonOrderList { get; set; } = new();
        public bool Judge(string destStr)
        {
            if (File.Exists(MainSave.AppDirectory + "JsonDeserize.json"))
                JsonOrderList = JsonConvert.DeserializeObject<List<JsonToDeserize>>(File.ReadAllText(MainSave.AppDirectory + "JsonDeserize.json"));
            return JsonOrderList.Any(x => destStr == x.Order && x.Enabled);
        }

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
                JsonToDeserize apiItem = JsonOrderList.Where(x => x.Order == e.Message.Text)
                                                                                .OrderBy(x => Guid.NewGuid().ToString()).FirstOrDefault();
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "JsonDeserizePic", apiItem.Order);
                Directory.CreateDirectory(targetdir);
                string imagename = DateTime.Now.ToString("yyyyMMddHHss") + ".jpg";
                string fullpath = Path.Combine(targetdir, imagename);
                using HttpWebClient http = new()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                };
                string url = apiItem.url, jsonpath = apiItem.picPath;
                JObject jObject = JObject.Parse(http.DownloadString(url));
                if (string.IsNullOrEmpty(apiItem.Text))
                {
                    sendText.MsgToSend.Add("获取内容为空，请重试");
                    return result;
                }

                string str = apiItem.Text;
                var c = Regex.Matches(apiItem.Text, "<.*?>");
                foreach (var item in c)
                {
                    string path = item.ToString().Replace("<", "").Replace(">", "");
                    str = str.Replace(item.ToString(), jObject.SelectToken(path).ToString());
                }
                e.FromGroup.SendGroupMessage(str);

                if (string.IsNullOrEmpty(jsonpath))
                {
                    MainSave.CQLog.Warning("Json解析接口", $"jsonPath为空，发生在 {apiItem.url} 接口中");
                    sendText.MsgToSend.Add("图片的Path为空，无法进行解析");
                    return result;
                }
                url = jObject.SelectToken(jsonpath).ToString();
                http.CookieCollection = new System.Net.CookieCollection();
                http.DownloadFile(url, fullpath);

                MainSave.CQLog.Info("Json解析接口", $"图片下载成功，尝试发送");

                string imagepath = Path.Combine("JsonDeserizePic", apiItem.Order, imagename);
                var msgItem = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(imagepath).ToSendString());
                if (apiItem.AutoRevoke)
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(AppConfig.R18_RevokeTime);
                        e.CQApi.RemoveMessage(msgItem.Id);
                    }).Start();
                }
            }
            catch (WebException exc)
            {
                sendText.MsgToSend.Add("网络错误，请重试");
                MainSave.CQLog.Info("Json解析接口", $"调用失败，错误信息：{exc.Message}\n{exc.StackTrace}");
                QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, 1);
            }
            catch (Exception exc)
            {
                sendText.MsgToSend.Add("Json解析接口调用失败");
                MainSave.CQLog.Info("Json解析接口", $"调用失败，错误信息：{exc.Message}\n{exc.StackTrace}");
                QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, 1);
            }
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
