﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class RankPic : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }

        public bool Judge(string destStr) => false;

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
