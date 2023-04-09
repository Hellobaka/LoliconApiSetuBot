using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;
using System.IO;
using System.Net;
using System.Reflection;

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

                ConfigHelper.InitConfig();
                if (AppConfig.ProxyEnabled)
                {
                    MainSave.Proxy = new WebProxy
                    {
                        Address = new Uri(AppConfig.ProxyURL),
                        Credentials = new NetworkCredential(AppConfig.ProxyUserName, AppConfig.ProxyPassword)
                    };
                }
                MainSave.InitPixivClient();
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
    }
}
