using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
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
                if (!MainSave.SauceNao_Saves.Any(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ))
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
            return TraceMoe_Call(CommonHelper.GetImageURL(imageCQCode.ToString()));
        }

        public static string TraceMoe_Call(string picURL)
        {
            string url = $"https://trace.moe/api/search?url={picURL}";
            using HttpWebClient http = new()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            };
            var json = JsonConvert.DeserializeObject<TraceMoe_Result.Data>(http.DownloadString(url));
            return json.ToString();
        }
    }
}
