using System;
using System.IO;
using System.Timers;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Tool.IniConfig;
using System.Net;
using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.Code.OrderFunctions;
using Native.Tool.IniConfig.Linq;
using System.Reflection;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using System.Collections.Generic;

namespace me.cqp.luohuaming.Setu.Code
{
    public class Event_StartUp : ICQStartup
    {
        public void CQStartup(object sender, CQStartupEventArgs e)
        {
            try
            {
                MainSave.AppDirectory = e.CQApi.AppDirectory;
                MainSave.CQApi = e.CQApi;
                MainSave.CQLog = e.CQLog;
                MainSave.ImageDirectory = CommonHelper.GetAppImageDirectory();
                ConfigHelper.ConfigFileName = Path.Combine(MainSave.AppDirectory, "Config.json");

                InitConfig();
                MainSave.InitPixivClient();
                if (AppConfig.ProxyEnabled)
                {
                    MainSave.Proxy = new WebProxy
                    {
                        Address = new Uri(AppConfig.ProxyURL),
                        Credentials = new NetworkCredential(AppConfig.ProxyUserName, AppConfig.ProxyPassword)
                    };
                }
                foreach (var item in Assembly.GetAssembly(typeof(Event_GroupMessage)).GetTypes())
                {
                    if (item.IsInterface)
                        continue;
                    foreach (var instance in item.GetInterfaces())
                    {
                        if (instance == typeof(IOrderModel))
                        {
                            IOrderModel obj = (IOrderModel)Activator.CreateInstance(item);
                            MainSave.Instances.Add(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainSave.CQLog.Warning("初始化失败", $"{ex.Message}\r\n{ex.StackTrace}");
            }
            QuotaHistory.CreateGroupQuotaDict();
        }

        private void InitConfig()
        {
            AppConfig.MaxGroupQuota = ConfigHelper.GetConfig("MaxGroupQuota", 50);
            AppConfig.MaxPersonQuota = ConfigHelper.GetConfig("MaxPersonQuota", 10);
            AppConfig.R18 = ConfigHelper.GetConfig("R18", false);
            AppConfig.R18_PicRevoke = ConfigHelper.GetConfig("R18_PicRevoke", true);
            AppConfig.R18_RevokeTime = ConfigHelper.GetConfig("R18_RevokeTime", 60 * 1000);
            AppConfig.ProxyEnabled = ConfigHelper.GetConfig("ProxyEnabled", false);
            AppConfig.ProxyURL = ConfigHelper.GetConfig("ProxyURL", "");
            AppConfig.ProxyUserName = ConfigHelper.GetConfig("ProxyUserName", "");
            AppConfig.ProxyPassword = ConfigHelper.GetConfig("ProxyPassword", "");
            AppConfig.PassSNI = ConfigHelper.GetConfig("PassSNI", true);
            AppConfig.SNI_IPAddress = ConfigHelper.GetConfig("SNI_IPAddress", "");
            AppConfig.PixivCookie = ConfigHelper.GetConfig("PixivCookie", "");
            AppConfig.PixivUA = ConfigHelper.GetConfig("PixivUA", "");
            AppConfig.WhiteMode = ConfigHelper.GetConfig("WhiteMode", false);
            AppConfig.Admin = ParseConfigList(ConfigHelper.GetConfig("Admin", ""));
            AppConfig.WhiteList = ParseConfigList(ConfigHelper.GetConfig("WhiteList", ""));
            AppConfig.BlackList = ParseConfigList(ConfigHelper.GetConfig("BlackList", ""));

            AppConfig.StartResponse = ConfigHelper.GetConfig("StartResponse", "");
            AppConfig.DownloadFailedResoponse = ConfigHelper.GetConfig("DownloadFailedResoponse", "");
            AppConfig.MaxMemberResoponse = ConfigHelper.GetConfig("MaxMemberResoponse", "");
            AppConfig.MaxGroupResoponse = ConfigHelper.GetConfig("MaxGroupResoponse", "");
            AppConfig.PicNotFoundResoponse = ConfigHelper.GetConfig("PicNotFoundResoponse", "");

            OrderConfig.LoliconPicOrder = ConfigHelper.GetConfig("LoliconPicOrder", "");
            OrderConfig.ClearLimitOrder = ConfigHelper.GetConfig("ClearLimitOrder", "");
            OrderConfig.PIDSearchOrder = ConfigHelper.GetConfig("PIDSearchOrder", "");
            OrderConfig.HotSearchOrder = ConfigHelper.GetConfig("HotSearchOrder", "");
            OrderConfig.SauceNaoSearchOrder = ConfigHelper.GetConfig("SauceNaoSearchOrder", "");
            OrderConfig.TraceMoeSearchOrder = ConfigHelper.GetConfig("TraceMoeSearchOrder", "");
            OrderConfig.YandereIDSearchOrder = ConfigHelper.GetConfig("YandereIDSearchOrder", "");
            OrderConfig.YandereTagSearchOrder = ConfigHelper.GetConfig("YandereTagSearchOrder", "");
        }
        public List<long> ParseConfigList(string s)
        {
            List<long> result = new();
            foreach (string item in s.Split('|'))
            {
                if (long.TryParse(item, out long admin))
                {
                    result.Add(admin);
                }
            }

            return result;
        }
    }
}
