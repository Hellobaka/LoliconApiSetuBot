using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code
{
    public class ItemToSave
    {
        /// <summary>
        /// 是否开启接口
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// 指令
        /// </summary>
        public string Order;
        /// <summary>
        /// API链接
        /// </summary>
        public string URL;
        /// <summary>
        /// API链接
        /// </summary>
        public string Path;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark;
        /// <summary>
        /// 调用次数限制
        /// </summary>
        public bool Usestrict;
        /// <summary>
        /// 自动撤回
        /// </summary>
        public bool AutoRevoke;
    }

}
