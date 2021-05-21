using System.Collections.Generic;
using System.Linq;
using System.Threading;
using me.cqp.luohuaming.Setu.Code;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace Native.Core
{
    public class MainExport : IGroupMessage, IPrivateMessage
    {
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            bool flag = false;
            for (int i = 0; i < MainSave.ConfigMain.Object["GroupList"]["Count"].GetValueOrDefault(0); i++)
            {
                if (e.FromGroup.Id == MainSave.ConfigMain.Object["GroupList"][$"Index{i}"].GetValueOrDefault(0))
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
            DelaySauceNao(e);
            DelayTraceMoe(e);

            FunctionResult result = Event_GroupMessage.GroupMessage(e);
            if (result.SendFlag)
            {
                if (result.SendObject == null || result.SendObject.Count == 0)
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
        }
        public void DelaySauceNao(CQGroupMessageEventArgs e)
        {
            //SauceNao调用判断
            List<DelayAPI_Save> ls = new List<DelayAPI_Save>();//保存副本,否则foreach会报错
            MainSave.SauceNao_Saves.ForEach(x => ls.Add(x));
            foreach (var item in ls)
            {
                if (item.GroupID == e.FromGroup.Id && item.QQID == e.FromQQ.Id)
                {
                    e.Handler = true;
                    if (e.Message.CQCodes.Where(x => x.IsImageCQCode).ToList().Count == 0)
                    {
                        MainSave.SauceNao_Saves.Remove(item);
                        e.FromGroup.SendGroupMessage("发送的不是图片，调用失败");
                        return;
                    }
                    else
                    {
                        MainSave.SauceNao_Saves.Remove(item);
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
        }
        public void DelayTraceMoe(CQGroupMessageEventArgs e)
        {
            List<DelayAPI_Save> ls = new List<DelayAPI_Save>();//保存副本,否则foreach会报错
            MainSave.TraceMoe_Saves.ForEach(x => ls.Add(x));
            foreach (var item in ls)
            {
                if (item.GroupID == e.FromGroup.Id && item.QQID == e.FromQQ.Id)
                {
                    e.Handler = true;
                    if (e.Message.CQCodes.Where(x => x.IsImageCQCode).ToList().Count == 0)
                    {
                        MainSave.TraceMoe_Saves.Remove(item);
                        e.FromGroup.SendGroupMessage("发送的不是图片，调用失败");
                        return;
                    }
                    else
                    {
                        MainSave.TraceMoe_Saves.Remove(item);
                        foreach (var cqCode in e.Message.CQCodes)
                        {
                            if (cqCode.IsImageCQCode)
                            {
                                string FunctionResult = me.cqp.luohuaming.Setu.Code.OrderFunctions.TraceMoe.TraceMoe_Call(cqCode);
                                e.FromGroup.SendGroupMessage(FunctionResult);
                                Thread.Sleep(1000);
                                return;
                            }
                        }
                    }
                    break;
                }
            }
        }
    }
}
