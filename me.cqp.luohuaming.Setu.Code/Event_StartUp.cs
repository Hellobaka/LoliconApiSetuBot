using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Tool.IniConfig;

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
            IniConfig ini = new IniConfig(e.CQApi.AppDirectory+"Config.ini");
            ini.Load();
            try
            {
                if (ini.Object["Proxy"]["IsEnabled"].GetValueOrDefault("0") == "1")
                { new Uri(ini.Object["Proxy"]["ProxyUri"].GetValueOrDefault("0")); }
            }
            catch(Exception ex)
            {
                e.CQLog.Warning("代理设置无效",$"代理参数有误，错误信息:{ex.Message}");
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
            if (DateTime.Now.Hour==0 && DateTime.Now.Minute == 0 && DateTime.Now.Second<=1)
            {
                if (File.Exists(CQSave.AppDirectory + "ConfigLimit.ini"))
                {
                    File.Delete(CQSave.AppDirectory + "ConfigLimit.ini");
                    CQSave.cqlog.Info("涩图机重置","限制已重置");
                }
            }
        }

    }
}
