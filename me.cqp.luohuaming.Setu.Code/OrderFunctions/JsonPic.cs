using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class JsonPic : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        static List<JsonToDeserize> JsonOrderList = new List<JsonToDeserize>();
        public bool Judge(string destStr)
        {
            if (File.Exists(MainSave.AppDirectory + "JsonDeserize.json"))
                JsonOrderList = JsonConvert.DeserializeObject<List<JsonToDeserize>>(File.ReadAllText(MainSave.AppDirectory + "JsonDeserize.json"));

            if (JsonOrderList.Count == 0 || string.IsNullOrWhiteSpace(destStr)) return false;
            foreach (var item in JsonOrderList)
                if (destStr == item.Order && item.Enabled)
                    return true;
            return false;
        }

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

            var functionResult = JsonDeserize_Image(e.Message.Text, e.FromGroup, e.FromQQ);
            if (functionResult == null)
            {
                return result;
            }
            var staues = e.FromGroup.SendGroupMessage(functionResult.MsgToSend[0]);
            if (functionResult.HandlingFlag)//自动撤回
            {
                IniConfig ini = MainSave.ConfigMain;
                Task task = new Task(() =>
                {
                    Thread.Sleep(PublicVariables.R18_RevokeTime * 1000);
                    e.CQApi.RemoveMessage(staues.Id);
                }); task.Start();
            }
            return result;
        }
        /// <summary>
        /// Json解析拉取
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static SendText JsonDeserize_Image(string orderText, long GroupID, long QQID)
        {
            SendText result = new SendText();
            try
            {
                //尝试拉取图片，若有多个相同的接口则随机来一个
                JsonToDeserize item = JsonOrderList.Where(x => x.Order == orderText)
                                                                            .OrderBy(x => Guid.NewGuid().ToString()).FirstOrDefault();
                result.HandlingFlag = item.AutoRevoke;
                //以后要用的路径,先生成一个
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "JsonDeserizePic", item.Order);
                if (!Directory.Exists(targetdir))
                {
                    Directory.CreateDirectory(targetdir);
                }
                string imagename = DateTime.Now.ToString("yyyyMMddHHss") + ".jpg";
                string fullpath = Path.Combine(targetdir, imagename);
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                })
                {
                    string url = item.url, jsonpath = item.picPath;
                    string json = Encoding.UTF8.GetString(http.DownloadData(url)).Replace('﻿', ' ');
                    JObject jObject = JObject.Parse(json);
                    if (!string.IsNullOrEmpty(item.Text))
                    {
                        string str = item.Text;
                        var c = Regex.Matches(item.Text, "<.*?>");
                        foreach (var item2 in c)
                        {
                            string path = item2.ToString().Replace("<", "").Replace(">", "");
                            str = str.Replace(item2.ToString(), jObject.SelectToken(path).ToString());
                        }
                        MainSave.CQApi.SendGroupMessage(GroupID, str);
                    }
                    if (string.IsNullOrEmpty(jsonpath))
                    {
                        MainSave.CQLog.Warning("Json解析接口", $"jsonPath为空，发生在 {item.url} 接口中");
                        return null;
                    }
                    url = jObject.SelectToken(jsonpath).ToString();
                    http.CookieCollection = new System.Net.CookieCollection();
                    http.DownloadFile(url, fullpath);
                }

                MainSave.CQLog.Info("Json解析接口", $"图片下载成功，尝试发送");
                //GetSetu.AntiHX(fullpath);
                string imagepath = Path.Combine("JsonDeserizePic", item.Order, imagename);
                result.MsgToSend.Add(CQApi.CQCode_Image(imagepath).ToSendString());
                return result;
            }
            catch (Exception exc)
            {
                result.MsgToSend.Add("Json解析接口调用失败");
                MainSave.CQLog.Info("Json解析接口", $"调用失败，错误信息：{exc.Message}");
                return result;
            }
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
