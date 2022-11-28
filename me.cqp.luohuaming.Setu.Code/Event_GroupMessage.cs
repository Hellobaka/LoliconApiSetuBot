using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Native.Sdk.Cqp.EventArgs;
using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;

namespace me.cqp.luohuaming.Setu.Code
{
    public class Event_GroupMessage
    {
        public static FunctionResult GroupMessage(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult()
            {
                SendFlag = false
            };
            try
            {
                if (AppConfig.WhiteMode)
                {
                    if (!AppConfig.WhiteList.Contains(e.FromGroup))
                    {
                        return result;
                    }
                }
                else
                {
                    if (AppConfig.BlackList.Contains(e.FromGroup))
                    {
                        return result;
                    }
                }
                DelaySauceNao(e);
                DelayTraceMoe(e);

                foreach (var item in MainSave.Instances.Where(item => item.Judge(e.Message.Text)))
                {
                    return item.Progress(e);
                }
                return result;
            }
            catch (Exception exc)
            {
                MainSave.CQLog.Info("异常抛出",exc.Message + exc.StackTrace);
                return result;
            }
        }
        public static void DelaySauceNao(CQGroupMessageEventArgs e)
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
                                OrderFunctions.SauceNao.SauceNao_Call(cqCode, e);
                                Thread.Sleep(1000);
                                return;
                            }
                        }
                    }
                    break;
                }
            }
        }
        public static void DelayTraceMoe(CQGroupMessageEventArgs e)
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
                                string FunctionResult = OrderFunctions.TraceMoe.TraceMoe_Call(cqCode);
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
