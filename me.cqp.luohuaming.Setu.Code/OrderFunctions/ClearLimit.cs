using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.EventArgs;
using System;
using System.IO;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class ClearLimit : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.ClearLimit))
            {
                PublicVariables.ClearLimit = Guid.NewGuid().ToString();
            }
            return PublicVariables.ClearLimit;
        }

        public bool Judge(string destStr)
        {
            return destStr.Replace(" ", "") == GetOrderStr();
        }

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult
            {
                Result = true,
                SendFlag = true,
            };
            SendText sendText = new SendText
            {
                SendID = e.FromGroup
            };
            if (!AppConfig.Admin.Contains(e.FromQQ.Id))
            {
                sendText.MsgToSend.Add("权限不足，拒绝操作");
            }
            else
            {
                QuotaHistory.ClearGroupHistory(e.FromGroup, DateTime.Now);
                sendText.MsgToSend.Add("重置成功");
            }
            result.SendObject.Add(sendText);
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
