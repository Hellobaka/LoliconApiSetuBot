using System.Collections.Generic;
using System.IO;
using System.Net;
using Native.Sdk.Cqp;
using Native.Tool.IniConfig;
using Scighost.PixivApi;

namespace me.cqp.luohuaming.Setu.PublicInfos
{
    public static class MainSave
    {
        /// <summary>
        /// 保存各种事件的数组
        /// </summary>
        public static List<IOrderModel> Instances { get; set; } = new List<IOrderModel>();
        public static CQLog CQLog { get; set; }
        public static CQApi CQApi { get; set; }
        public static string AppDirectory { get; set; }
        public static string ImageDirectory { get; set; }
        public static WebProxy Proxy { get; set; }
        public static List<DelayAPI_Save> SauceNao_Saves { get; set; } = new List<DelayAPI_Save>();
        public static List<DelayAPI_Save> TraceMoe_Saves { get; set; } = new List<DelayAPI_Save>();
        public static PixivClient PixivClient { get; set; }

        static IniConfig configMain;
        public static IniConfig ConfigMain
        {
            get
            {
                if (configMain != null)
                    return configMain;
                configMain = new IniConfig(Path.Combine(AppDirectory, "Config.ini"));
                configMain.Load();
                return configMain;
            }
            set { configMain = value; }
        }
        public static bool changeFlag { get; set; } = false;
        static IniConfig configLimit;
        public static IniConfig ConfigLimit
        { 
            get
            {
                if (changeFlag)
                    return configLimit;
                configLimit = new IniConfig(Path.Combine(AppDirectory, "ConfigLimit.ini"));
                configLimit.Load();
                return configLimit;
            }
            set { configLimit = value;}
        }
    }
}
