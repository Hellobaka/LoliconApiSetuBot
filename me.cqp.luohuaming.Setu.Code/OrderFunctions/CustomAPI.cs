using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class CustomAPI : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 自定义接口指令数组
        /// </summary>
        List<CustomObject> CustomAPIOrderList { get; set; } = new();
        public bool Judge(string destStr)
        {
            if (File.Exists(MainSave.AppDirectory + "CustomAPI.json"))
                CustomAPIOrderList = JsonConvert.DeserializeObject<List<CustomObject>>(File.ReadAllText(MainSave.AppDirectory + "CustomAPI.json"));
            return CustomAPIOrderList.Any(x => destStr == x.Order && x.Enabled);
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
                CustomObject apiItem = CustomAPIOrderList.Where(x => x.Order == e.Message.Text)
                                                                            .OrderBy(x => Guid.NewGuid().ToString()).FirstOrDefault();
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "CustomAPIPic", apiItem.Order);
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
                http.DownloadFile(apiItem.URL, fullpath);
                MainSave.CQLog.Info("自定义接口", $"图片下载成功，尝试发送");

                string imagepath = Path.Combine("CustomAPIPic", apiItem.Order, imagename);
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
            catch (Exception exc)
            {
                sendText.MsgToSend.Add($"网络图片接口调用失败, {exc.Message}");
                e.CQLog.Info("网络图片接口", exc.Message + exc.StackTrace);
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
