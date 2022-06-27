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

namespace me.cqp.luohuaming.Setu.Code
{
    public class Event_StartUp : ICQStartup
    {
        public void CQStartup(object sender, CQStartupEventArgs e)
        {
            MainSave.AppDirectory = e.CQApi.AppDirectory;
            MainSave.CQApi = e.CQApi;
            MainSave.CQLog = e.CQLog;
            MainSave.ImageDirectory = CommonHelper.GetAppImageDirectory();

            IniConfig ini = new IniConfig(e.CQApi.AppDirectory + "Config.ini");
            ini.Load();
            MainSave.ConfigMain = ini;
            PublicVariables.ReadOrderandAnswer();
            try
            {
                WebProxy proxy = null;
                if (ini.Object["Proxy"]["IsEnabled"].GetValueOrDefault(0) == 1)
                {
                    //代理设置
                    string uri, username, pwd;
                    uri = ini.Object["Proxy"]["ProxyUri"].GetValueOrDefault("");
                    username = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("");
                    pwd = ini.Object["Proxy"]["ProxyPwd"].GetValueOrDefault("");
                    proxy = new WebProxy
                    {
                        Address = new Uri(uri),
                        Credentials = new NetworkCredential(username, pwd)
                    };
                    MainSave.Proxy = proxy;
                }
            }
            catch (Exception ex)
            {
                MainSave.CQLog.Warning("代理设置无效", $"代理参数有误，错误信息:{ex.Message}");
            }

            ini = MainSave.ConfigLimit;
            if (ini.Object == null || ini.Object.Count == 0)
            {
                File.WriteAllText(e.CQApi.AppDirectory + "ConfigLimit.ini", "[Config]\nTimestamp=1608773153");
                ini.Load();
            }
            if (JudgeifTimestampOverday(ini.Object["Config"]["Timestamp"].GetValueOrDefault(0), CommonHelper.GetTimeStamp()))
            {
                if (File.Exists(MainSave.AppDirectory + "ConfigLimit.ini"))
                {
                    File.WriteAllText(MainSave.AppDirectory + "ConfigLimit.ini", "[Config]\nTimestamp=1608773153");
                    MainSave.CQLog.Info("涩图机重置", "限制已重置");
                }
            }
            timersTimer.Interval = 1000;
            timersTimer.Enabled = true;
            timersTimer.Elapsed += TimersTimer_Elapsed;
            timersTimer.Start();

            MainSave.Instances.Add(new ClearLimit());
            MainSave.Instances.Add(new CustomAPI());
            MainSave.Instances.Add(new GetLoliconPic());
            MainSave.Instances.Add(new JsonPic());
            MainSave.Instances.Add(new LocalPic());
            MainSave.Instances.Add(new PIDSearch());
            MainSave.Instances.Add(new SauceNao());
            MainSave.Instances.Add(new TraceMoe());
            MainSave.Instances.Add(new YandeRePic());
            MainSave.Instances.Add(new YandeReTagSearch());
        }
        private static Timer timersTimer = new Timer();

        public static void TimersTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second <= 1)
            {
                if (File.Exists(MainSave.AppDirectory + "ConfigLimit.ini"))
                {
                    File.WriteAllText(MainSave.AppDirectory + "ConfigLimit.ini", "[Config]\nTimestamp=1608773153");
                    MainSave.CQLog.Info("涩图机重置", "限制已重置");
                }
            }
        }

        /// <summary>
        /// 判断两个时间戳是否隔天
        /// </summary>
        /// <param name="dt1">用于判断的时间戳</param>
        /// <param name="dt2">实时时间戳</param>
        public static bool JudgeifTimestampOverday(long dt1, long dt2)
        {
            long Time2, testTimeLingchen, Time1;
            testTimeLingchen = dt1 - ((dt1 + 8 * 3600) % 86400);
            if (dt2 > dt1)
            {
                if (dt2 - testTimeLingchen > 24 * 60 * 60)
                {
                    return true;//是明天
                }
                else
                {
                    return false;//是今天
                }
            }
            else
            {
                return true;//是昨天
            }
        }

    }
}
