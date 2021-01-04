using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            //SauceNao调用判断
            List<SauceNao_Save> ls = new List<SauceNao_Save>();//保存副本,否则foreach会报错
            MainSave.sauceNao_Saves.ForEach(x => ls.Add(x));
            foreach (var item in ls)
            {
                if (item.GroupID == e.FromGroup.Id && item.QQID == e.FromQQ.Id)
                {
                    if (e.Message.CQCodes.Where(x => x.IsImageCQCode).ToList().Count == 0)
                    {
                        MainSave.sauceNao_Saves.Remove(item);
                        e.FromGroup.SendGroupMessage("发送的不是图片，调用失败");
                        return;
                    }
                    else
                    {
                        MainSave.sauceNao_Saves.Remove(item);
                        foreach (var cqCode in e.Message.CQCodes)
                        {
                            if (cqCode.IsImageCQCode)
                            {
                                me.cqp.luohuaming.Setu.Code.OrderFunctions.SauceNao.SauceNao_Call(cqCode, e);
                                Thread.Sleep(1000);
                                return;
                            }
                        }
                    }
                    break;
                }
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
