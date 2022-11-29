using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Native.Sdk.Cqp.EventArgs;
using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.Model;
using System.Reflection.Emit;

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
                if (!AppConfig.GroupList.Contains(e.FromGroup))
                {
                    return result;
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
                MainSave.CQLog.Info("异常抛出", exc.Message + exc.StackTrace);
                return result;
            }
        }

        public static void DelaySauceNao(CQGroupMessageEventArgs e)
        {
            if (!MainSave.SauceNao_Saves.Any(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ)) return;
            e.Handler = true;
            CQCode img = e.Message.CQCodes.FirstOrDefault(x => x.IsImageCQCode);

            if (img == null)
            {
                MainSave.TraceMoe_Saves.Remove(MainSave.SauceNao_Saves.First(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ));
                e.FromGroup.SendGroupMessage("发送的不是图片，调用失败");
                return;
            }
            else
            {
                MainSave.TraceMoe_Saves.Remove(MainSave.SauceNao_Saves.First(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ));
                OrderFunctions.SauceNao.SauceNao_Call(img, e);
            }
        }

        public static void DelayTraceMoe(CQGroupMessageEventArgs e)
        {
            if (!MainSave.SauceNao_Saves.Any(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ)) return;
            e.Handler = true;
            CQCode img = e.Message.CQCodes.FirstOrDefault(x => x.IsImageCQCode);

            if (img == null)
            {
                MainSave.TraceMoe_Saves.Remove(MainSave.SauceNao_Saves.First(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ));
                e.FromGroup.SendGroupMessage("发送的不是图片，调用失败");
                return;
            }
            else
            {
                MainSave.TraceMoe_Saves.Remove(MainSave.SauceNao_Saves.First(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ));
                string FunctionResult = OrderFunctions.TraceMoe.TraceMoe_Call(img);
                e.FromGroup.SendGroupMessage(FunctionResult);
            }
        }
    }
}

