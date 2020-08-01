using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code
{
    public class SauceNao_Save
    {
        public SauceNao_Save(long groupID, long qQID)
        {
            GroupID = groupID;
            QQID = qQID;
        }

        public long GroupID { get; set; }
        public long QQID { get; set; }
    }
}
