using System.Collections.Generic;
using System.Net;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
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
        public static PixivClient PixivClient { get; set; } = null;

        public static void InitPixivClient()
        {
            PixivClient = new PixivClient(cookie: AppConfig.PixivCookie, userAgent: AppConfig.PixivUA
                , bypassSNI: AppConfig.PassSNI, ip: AppConfig.SNI_IPAddress, httpProxy: Proxy);
        }
    }
}
