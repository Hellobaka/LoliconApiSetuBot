using me.cqp.luohuaming.Setu.Code.Helper;
using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.API;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.EventArgs;
using Scighost.PixivApi.Illust;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class PIDSearch : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.PIDSearch))
            {
                PublicVariables.PIDSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.PIDSearch;
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
            //检查额度限制
            if (QuotaHelper.QuotaCheck(e.FromGroup, e.FromQQ) is false)
            {
                return result;
            }
            PublicVariables.ReadOrderandAnswer();

            SendText sendText = new();
            sendText.SendID = e.FromGroup;
            result.SendObject.Add(sendText);
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
            result.SendFlag = false;
            e.FromGroup.SendGroupMessage($"正在查询pid={pid}的插画信息，请等待……");
            IllustInfo illustInfo = PixivAPI.GetPicInfo(pid);
            e.FromGroup.SendGroupMessage($"");
            if (illustInfo.Tags.Any(x=>x.Name.Contains("R-18")))
            {
                if (AppConfig.R18 is false)
                {
                    sendText.MsgToSend.Add("限制图片。");
                    return result;
                }

                var message = e.FromGroup.SendGroupMessage(PixivAPI.DownloadPic(pid, Path.Combine(MainSave.ImageDirectory, "PIDSearch")));
                if (AppConfig.R18_PicRevoke is false) return result;
                Task task = new(() =>
                {
                    Thread.Sleep(AppConfig.R18_RevokeTime);
                    e.CQApi.RemoveMessage(message.Id);
                }); task.Start();
            }
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
