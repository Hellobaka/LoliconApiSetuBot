using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.PublicInfos
{
    /// <summary>
    /// 配置读取帮助类
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static string ConfigFileName = @"conf/Config.json";

        public static object ReadLock { get; set; } = new object();
        public static object WriteLock { get; set; } = new object();

        /// <summary>
        /// 读取配置
        /// </summary>
        /// <param name="sectionName">需要读取的配置键名</param>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>目标类型的配置</returns>
        public static T GetConfig<T>(string sectionName, T defaultValue = default)
        {
            lock (ReadLock)
            {
                if (File.Exists(ConfigFileName) is false)
                    File.WriteAllText(ConfigFileName, "{}");
                var o = JObject.Parse(File.ReadAllText(ConfigFileName));
                if (o.ContainsKey(sectionName))
                    return o[sectionName].ToObject<T>();
                if (defaultValue != null)
                {
                    SetConfig<T>(sectionName, defaultValue);
                    return defaultValue;
                }

                if (typeof(T) == typeof(string))
                    return (T)(object)"";
                if (typeof(T) == typeof(int))
                    return (T)(object)0;
                if (typeof(T) == typeof(long))
                    return default;
                if (typeof(T) == typeof(bool))
                    return (T)(object)false;
                if (typeof(T) == typeof(object))
                    return (T)(object)new { };
                throw new Exception("无法默认返回");
            }
        }

        public static void SetConfig<T>(string sectionName, T value)
        {
            lock (WriteLock)
            {
                if (File.Exists(ConfigFileName) is false)
                    File.WriteAllText(ConfigFileName, "{}");
                var o = JObject.Parse(File.ReadAllText(ConfigFileName));
                if (o.ContainsKey(sectionName))
                {
                    o[sectionName] = JToken.FromObject(value);
                }
                else
                {
                    o.Add(sectionName, JToken.FromObject(value));
                }

                File.WriteAllText(ConfigFileName, o.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }

        public static bool ConfigHasKey(string sectionName)
        {
            if (File.Exists(ConfigFileName) is false)
                File.WriteAllText(ConfigFileName, "{}");
            var o = JObject.Parse(File.ReadAllText(ConfigFileName));
            return o.ContainsKey(sectionName);
        }

        public static void InitConfig()
        {
            AppConfig.MaxGroupQuota = GetConfig("MaxGroupQuota", 50);
            AppConfig.MaxPersonQuota = GetConfig("MaxPersonQuota", 10);
            AppConfig.R18 = GetConfig("R18", false);
            AppConfig.R18_PicRevoke = GetConfig("R18_PicRevoke", false);
            AppConfig.R18_RevokeTime = GetConfig("R18_RevokeTime", 60 * 1000);
            AppConfig.AntiBan = GetConfig("AntiBan", false);
            AppConfig.AntiBanType = (AntiBanType)GetConfig("AntiBanType", 0);
            AppConfig.ProxyEnabled = GetConfig("ProxyEnabled", false);
            AppConfig.ProxyURL = GetConfig("ProxyURL", "");
            AppConfig.ProxyUserName = GetConfig("ProxyUserName", "");
            AppConfig.ProxyPassword = GetConfig("ProxyPassword", "");
            AppConfig.PassSNI = GetConfig("PassSNI", true);
            AppConfig.SNI_IPAddress = GetConfig("SNI_IPAddress", "");
            AppConfig.PixivCookie = GetConfig("PixivCookie", "");
            AppConfig.PixivUA = GetConfig("PixivUA", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            AppConfig.AdminList = ParseConfigList(GetConfig("AdminList", ""));
            AppConfig.GroupList = ParseConfigList(GetConfig("GroupList", ""));

            AppConfig.StartResponse = GetConfig("StartResponse", "拉取图片中~至少需要15s……\n你今日剩余调用次数为<count>次(￣▽￣)");
            AppConfig.MaxMemberResoponse = GetConfig("MaxMemberResoponse", "你当日所能调用的次数已达上限(￣▽￣)");
            AppConfig.MaxGroupResoponse = GetConfig("MaxGroupResoponse", "本群当日所能调用的次数已达上限(￣▽￣)");
            AppConfig.PicNotFoundResoponse = GetConfig("PicNotFoundResoponse", "哦淦 老兄你的xp好机八小众啊 找不到啊");
            AppConfig.SuccessResponse = GetConfig("SuccessResponse", "title: <title>\nauthor: <author>\np: <p>\npid: <pid>");

            OrderConfig.LoliconPicOrder = GetConfig("LoliconPicOrder", "#setu");
            OrderConfig.ClearLimitOrder = GetConfig("ClearLimitOrder", "#clear");
            OrderConfig.PIDSearchOrder = GetConfig("PIDSearchOrder", "#pid");
            OrderConfig.SauceNaoSearchOrder = GetConfig("SauceNaoSearchOrder", "#nao");
            OrderConfig.TraceMoeSearchOrder = GetConfig("TraceMoeSearchOrder", "#trace");
            OrderConfig.YandereIDSearchOrder = GetConfig("YandereIDSearchOrder", "#yid");
            OrderConfig.YandereTagSearchOrder = GetConfig("YandereTagSearchOrder", "#ytag");

            QuotaHistory.CreateGroupQuotaDict();
        }

        public static List<long> ParseConfigList(string s)
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
