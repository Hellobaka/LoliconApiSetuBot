using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class LocalPic : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 本地图片指令列表
        /// </summary>
        static List<CustomObject> LocalPicOrderList { get; set; } = new();
        public bool Judge(string destStr)
        {
            if (File.Exists(MainSave.AppDirectory + "LocalPic.json"))
                LocalPicOrderList = JsonConvert.DeserializeObject<List<CustomObject>>(File.ReadAllText(MainSave.AppDirectory + "LocalPic.json"));
            return LocalPicOrderList.Any(x => destStr == x.Order && x.Enabled);
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
                CustomObject apiItem = LocalPicOrderList.Where(x => x.Order == e.Message.Text)
                                                                                       .OrderBy(_ => Guid.NewGuid()).First();
                DirectoryInfo directoryInfo = new(apiItem.Path);
                var picinfo = directoryInfo.GetFiles("*.*")
                    .Where(x => x.FullName.EndsWith("jpg") || x.FullName.EndsWith("gif")
                    || x.FullName.EndsWith("png") || x.FullName.EndsWith("bmp")
                    || x.FullName.EndsWith("webp") || x.FullName.EndsWith("tif") || x.FullName.EndsWith("tga"))
                    .OrderBy(_ => Guid.NewGuid()).First();
                string picpathOrigin = picinfo.FullName;
                Directory.CreateDirectory(MainSave.ImageDirectory + "\\LocalPic");
                string picpathFinal = MainSave.ImageDirectory + "\\LocalPic\\" + picinfo.Name;
                if (!File.Exists(picpathFinal))
                    File.Copy(picpathOrigin, picpathFinal);
                var msgItem = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(Path.Combine("LocalPic", picinfo.Name)).ToSendString());
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
                sendText.MsgToSend.Add($"本地图片调用失败, {exc.Message}");
                e.CQLog.Info("本地图片", exc.Message + exc.StackTrace);
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
