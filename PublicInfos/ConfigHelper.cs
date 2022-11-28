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
    }
}
