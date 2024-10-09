using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class TraceMoe : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(OrderConfig.TraceMoeSearchOrder))
            {
                return Guid.NewGuid().ToString();
            }
            return OrderConfig.TraceMoeSearchOrder;
        }

        public bool Judge(string destStr)
        {
            return destStr.Replace("＃", "#").StartsWith(GetOrderStr());
        }

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new()
            {
                Result = true,
                SendFlag = true,
            };
            SendText sendText = new()
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
                if (MainSave.SauceNao_Saves.Any(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ))
                {
                    CQCode img = e.Message.CQCodes.FirstOrDefault(x => x.IsImageCQCode);
                    if (img == null) return result;
                    sendText.MsgToSend.Add(TraceMoe_Call(img));
                }
                else
                {
                    int quota = AppConfig.MaxPersonQuota - QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, -1);
                    e.FromGroup.SendGroupMessage(AppConfig.StartResponse.Replace("<count>", quota.ToString()));

                    result.SendFlag = true;
                    MainSave.TraceMoe_Saves.Add(new DelayAPI_Save(e.FromGroup.Id, e.FromQQ.Id));
                    sendText.MsgToSend.Add("请在接下来的一条消息内发送需要搜索的图片");
                }
            }
            catch (Exception exc)
            {
                sendText.MsgToSend.Add($"解析出错，错误信息：{exc.Message}");
                e.CQLog.Info("Tracemoe", exc.Message + "\n" + exc.StackTrace);
            }
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static string TraceMoe_Call(CQCode imageCQCode)
        {
            string path = MainSave.CQApi.ReceiveImage(imageCQCode);
            return TraceMoe_Call(path);
        }

        public static string TraceMoe_Call(string picPath)
        {
            string url = $"https://api.trace.moe/search?anilistInfo";
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = MainSave.Proxy,
                UseProxy = true
            };
            using HttpClient http = new(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(10),
            };

            using var form = new MultipartFormDataContent();
            var fileStream = new FileStream(picPath, FileMode.Open, FileAccess.Read);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/jpeg");

            // 添加文件到请求
            form.Add(fileContent, "image", Path.GetFileName(picPath));
            var t = http.PostAsync(url, form).Result;
            t.EnsureSuccessStatusCode();
            
            var json = JsonConvert.DeserializeObject<TraceMoe_Result>(t.Content.ReadAsStringAsync().Result);
            return json.ToString();
        }
    }
}
