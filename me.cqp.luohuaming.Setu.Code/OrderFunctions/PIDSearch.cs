using System;
using System.Threading;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.IniConfig;
using PublicInfos;

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
            SendText sendText = new SendText();
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
            IllustInfo illustInfo = PixivAPI.GetIllustInfo(pid);
            e.FromGroup.SendGroupMessage(illustInfo.IllustText);
            var message = e.FromGroup.SendGroupMessage(illustInfo.IllustCQCode);
            if (illustInfo.R18_Flag)
            {
                IniConfig ini = MainSave.ConfigMain;
                Task task = new Task(() =>
                {
                    Thread.Sleep(ini.Object["R18"]["RevokeTime"] * 1000);
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
