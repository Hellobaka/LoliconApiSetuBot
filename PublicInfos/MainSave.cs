using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Tool.IniConfig;

namespace PublicInfos
{
    public static class MainSave
    {
        /// <summary>
        /// 保存各种事件的数组
        /// </summary>
        public static List<IOrderModel> Instances { get; set; } = new List<IOrderModel>();
        public static CQLog CQLog { get; set; }
        public static CQApi CQApi { get; set; }
        public static string AppDirectroy { get; set; }
        public static string ImageDirectory { get; set; }
        public static WebProxy Proxy { get; set; }
        static IniConfig configMain;
        public static IniConfig ConfigMain
        {
            get
            {
                configMain = new IniConfig(Path.Combine(AppDirectroy, "Config.ini"));
                configMain.Load();
                return configMain;
            }
            set { configMain = value; }
        }
        static IniConfig configLimit;
        public static IniConfig ConfigLimit
        { 
            get
            {
                configLimit = new IniConfig(Path.Combine(AppDirectroy, "ConfigLimit.ini"));
                configLimit.Load();
                return configLimit;
            }
            set { configLimit = value;}
        }
    }
}
