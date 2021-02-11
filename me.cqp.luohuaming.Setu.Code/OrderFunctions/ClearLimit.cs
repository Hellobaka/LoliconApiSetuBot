using System;
using System.IO;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.IniConfig;
using PublicInfos;

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
                 SendID=e.FromGroup
            };
            if (!CommonHelper.CheckAdmin(e.FromQQ.Id))
            {
                sendText.MsgToSend.Add("权限不足，拒绝操作");
            }
            else
            {
                File.WriteAllText(MainSave.AppDirectory + "ConfigLimit.ini", "[Config]\nTimestamp=1608773153");
                sendText.MsgToSend.Add("重置成功");
            }
            return result;
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
