using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.API;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Scighost.PixivApi.Illust;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class PIDSearch : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(OrderConfig.PIDSearchOrder))
            {
                return Guid.NewGuid().ToString();
            }
            return OrderConfig.PIDSearchOrder;
        }

        public bool Judge(string destStr)
        {
            return destStr.ToLower().StartsWith(GetOrderStr());
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

            if (e.Message.Text.Trim().Length == GetOrderStr().Length)
            {
                sendText.MsgToSend.Add("指令无效，请在指令后添加pid");
                return result;
            }
            if (!int.TryParse(e.Message.Text.Substring(GetOrderStr().Length).Replace(" ", ""), out int pid))
            {
                sendText.MsgToSend.Add("指令无效，检查是否为纯数字");
                return result;
            }
            try
            {
                int quota = AppConfig.MaxPersonQuota - QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, -1);
                e.FromGroup.SendGroupMessage(AppConfig.StartResponse.Replace("<count>", quota.ToString()));

                e.FromGroup.SendGroupMessage($"正在查询pid={pid}的插画信息，请等待……");
                IllustInfo illustInfo = PixivAPI.GetPicInfo(pid);
                e.FromGroup.SendGroupMessage($"title: {illustInfo.Title}\nauthor: {illustInfo.UserName}\n共有: {illustInfo.PageCount}p\npid: {pid}");
                bool r18Flag = illustInfo.Tags.Any(x => x.Name.Contains("R-18"));
                if (r18Flag)
                {
                    if (AppConfig.R18 is false)
                    {
                        sendText.MsgToSend.Add("限制图片。");
                        return result;
                    }
                }
                int msgId = 0;
                string filename = new DirectoryInfo(Path.Combine(MainSave.ImageDirectory, "PIDSearch")).GetFiles().FirstOrDefault(x => x.Name.Contains(pid.ToString()))?.Name;
                if (string.IsNullOrEmpty(filename))
                {
                    var fileInfo = new FileInfo(PixivAPI.DownloadPic(pid, Path.Combine(MainSave.ImageDirectory, "PIDSearch")));
                    var c = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(@"PIDSearch\" + fileInfo.Name).ToSendString());
                    msgId = c.Id;
                }
                else
                {
                    var c = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(@"PIDSearch\" + filename).ToSendString());
                    msgId = c.Id;
                }

                if (AppConfig.R18_PicRevoke && r18Flag)
                {
                    Task task = new(() =>
                    {
                        Thread.Sleep(AppConfig.R18_RevokeTime);
                        e.CQApi.RemoveMessage(msgId);
                    }); task.Start();
                }
            }
            catch (WebException exc)
            {
                e.CQLog.Info("PIDSearch", exc.Message + exc.StackTrace);
                sendText.MsgToSend.Add($"网络错误，请重试");
                QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, 1);
            }
            catch (Exception exc)
            {
                if(exc.InnerException != null)
                {
                    e.CQLog.Info("PIDSearch", exc.InnerException.Message + exc.InnerException.StackTrace);
                    sendText.MsgToSend.Add($"发生错误: {exc.InnerException.Message}");
                }
                else
                {
                    e.CQLog.Info("PIDSearch", exc.Message + exc.StackTrace);
                    sendText.MsgToSend.Add($"发生错误: {exc.Message}");
                }
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
