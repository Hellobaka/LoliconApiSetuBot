using System;
using System.IO;
using System.Timers;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Tool.IniConfig;
using System.Net;

namespace me.cqp.luohuaming.Setu.Code
{
    public class Event_StartUp : ICQStartup
    {
        public void CQStartup(object sender, CQStartupEventArgs e)
        {
            CQSave.AppDirectory = e.CQApi.AppDirectory;
            CQSave.ImageDirectory = GetAppImageDirectory(e.CQApi.AppDirectory);
            CQSave.cq = e.CQApi;
            CQSave.cqlog = e.CQLog;
            IniConfig ini = new IniConfig(e.CQApi.AppDirectory + "Config.ini");
            ini.Load();
            if (ini.Object["R18"]["Enabled"].GetValueOrDefault("0") == "1")
                CQSave.R18 = true;

            try
            {
                WebProxy proxy = null;
                if (ini.Object["Proxy"]["IsEnabled"].GetValueOrDefault("0") == "1")
                {
                    //代理设置
                    string uri, username, pwd;
                    uri = ini.Object["Proxy"]["ProxyUri"].GetValueOrDefault("0");
                    username = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("0");
                    pwd = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("0");
                    proxy = new WebProxy
                    {
                        Address = new Uri(uri),
                        Credentials = new NetworkCredential(username, pwd)
                    };
                    CQSave.proxy = proxy;
                }
            }
            catch (Exception ex)
            {
                e.CQLog.Warning("代理设置无效", $"代理参数有误，错误信息:{ex.Message}");
            }
            ini = new IniConfig(e.CQApi.AppDirectory + "ConfigLimit.ini");
            ini.Load();
            if (JudgeifTimestampOverday(ini.Object["Config"]["Timestamp"].GetValueOrDefault(0), GetTimeStamp()))
            {
                if (File.Exists(CQSave.AppDirectory + "ConfigLimit.ini"))
                {
                    File.Delete(CQSave.AppDirectory + "ConfigLimit.ini");
                    CQSave.cqlog.Info("涩图机重置", "限制已重置");
                }
            }
            timersTimer.Interval = 1000;
            timersTimer.Enabled = true;
            timersTimer.Elapsed += TimersTimer_Elapsed;
            timersTimer.Start();
        }
        public static string GetAppImageDirectory(string dir)
        {
            var ImageDirectory = Path.Combine(Environment.CurrentDirectory, "data", "image\\");
            return ImageDirectory;
        }
        //获取时间戳
        public static Int64 GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        private static Timer timersTimer = new Timer();

        public static void TimersTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second <= 1)
            {
                if (File.Exists(CQSave.AppDirectory + "ConfigLimit.ini"))
                {
                    File.Delete(CQSave.AppDirectory + "ConfigLimit.ini");
                    CQSave.cqlog.Info("涩图机重置", "限制已重置");
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
