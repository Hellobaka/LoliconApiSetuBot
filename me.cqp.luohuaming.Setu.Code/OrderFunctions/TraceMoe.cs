using System;
using System.Text;
using System.Threading;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class TraceMoe : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.TraceMoeSearch))
            {
                PublicVariables.TraceMoeSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.TraceMoeSearch;
        }

        public bool Judge(string destStr)
        {
            return destStr.Replace("＃", "#").StartsWith(GetOrderStr());
        }

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult()
            {
                Result = true,
                SendFlag = true,
            };
            SendText sendText = new SendText();
            sendText.SendID = e.FromGroup;
            result.SendObject.Add(sendText);

            if (e.Message.CQCodes.Count != 0)
            {
                foreach (var item in e.Message.CQCodes)
                {
                    if (item.IsImageCQCode)
                    {
                        string FunctionResult = TraceMoe_Call(item);
                        if (FunctionResult != null)
                        {
                            sendText.MsgToSend.Add(FunctionResult);
                        }
                    }
                }
            }
            else
            {
                result.SendFlag = true;
                MainSave.TraceMoe_Saves.Add(new DelayAPI_Save(e.FromGroup.Id, e.FromQQ.Id));
                sendText.MsgToSend.Add("请在接下来的一条消息内发送需要搜索的图片");
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
                    var json = JsonConvert.DeserializeObject<TraceMoe_Result.Data>(http.DownloadString(url));
                    return json.ToString();
                }
                catch(Exception e)
                {
                    MainSave.CQLog.Info("解析出错", e.Message,"\n",e.StackTrace);
                    return "解析出错，详情请看日志";
                }
            }
        }
    }
}
