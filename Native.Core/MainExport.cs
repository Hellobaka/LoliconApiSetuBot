﻿using System.Collections.Generic;
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
    }
}
