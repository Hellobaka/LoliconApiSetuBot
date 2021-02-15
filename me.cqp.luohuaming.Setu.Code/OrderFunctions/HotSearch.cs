using System;
using System.Threading;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.IniConfig;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class HotSearch : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.HotSearch))
            {
                PublicVariables.HotSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.HotSearch;
        }

        public bool Judge(string destStr)
        {
            return destStr.Trim().StartsWith(GetOrderStr());
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

            SendText sendText = new SendText();
            sendText.SendID = e.FromGroup;
            result.SendObject.Add(sendText);

            string keyword = e.Message.Text.Replace(" ", "").Substring(GetOrderStr().Length);
            e.FromGroup.SendGroupMessage($"正在查询关键字为{keyword}的插画信息，请等待……");
            IllustInfo illustInfo = PixivAPI.GetHotSearch(keyword);
            e.FromGroup.SendGroupMessage(illustInfo.IllustText);
            var message = e.FromGroup.SendGroupMessage(illustInfo.IllustCQCode);
            if (illustInfo.R18_Flag)
            {
                IniConfig ini = MainSave.ConfigMain;
                Task task = new Task(() =>
                {
                    Thread.Sleep(PublicVariables.R18_RevokeTime * 1000);
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
