using me.cqp.luohuaming.Setu.Code;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using PublicInfos;

namespace Native.Core
{
    public class MainExport: IGroupMessage,IPrivateMessage
    {
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            bool flag = false;
            for (int i = 0; i < MainSave.ConfigMain.Object["GroupList"]["Count"].GetValueOrDefault(0); i++)
            {
                if (e.FromGroup.Id == MainSave.ConfigMain.Object["GroupList"][$"Item{i}"].GetValueOrDefault(0))
                {
                    flag = true;
                    break;
                }
            }
            if (flag is false)
            {
                e.Handler = false;
                return;
            }
            FunctionResult result = Event_GroupMessage.GroupMessage(e);
            if (result.SendFlag)
            {
                if (result.SendObject == null)
                {
                    e.Handler = false;
                }
                foreach (var item in result.SendObject)
                {
                    foreach (var sendMsg in item.MsgToSend)
                    {
                        e.CQApi.SendGroupMessage(item.SendID, sendMsg);
                    }
                }
            }
            e.Handler = result.Result;
        }

        public void PrivateMessage(object sender, CQPrivateMessageEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
