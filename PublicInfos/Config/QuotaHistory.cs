using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace me.cqp.luohuaming.Setu.PublicInfos.Config
{
    public class QuotaHistory
    {
        public static Dictionary<long, int> GroupQuotaDict = new();
        public static int QueryQuota(long group, long qq)
        {
            string date = DateTime.Now.ToString("d");
            string path = Path.Combine(MainSave.AppDirectory, "Quota.json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }
            JArray json = JArray.Parse(File.ReadAllText(path));
            foreach (var jToken in json)
            {
                var item = (JObject)jToken;
                if (item["group"].ToObject<long>() == group && item["qq"].ToObject<long>() == qq)
                {
                    JObject data = (JObject)item["data"];
                    if (data.ContainsKey(date))
                    {
                        return AppConfig.MaxPersonQuota - data[date].ToObject<int>();
                    }
                    else
                    {
                        return AppConfig.MaxPersonQuota;
                    }
                }
            }

            return AppConfig.MaxPersonQuota;
        }

        public static int HandleQuota(long group, long qq, int change)
        {
            string date = DateTime.Now.ToString("d");
            string path = Path.Combine(MainSave.AppDirectory, "Quota.json");
            int finalQuota = 0;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }
            JArray json = JArray.Parse(File.ReadAllText(path));
            bool findFlag = false;
            foreach (var jToken in json)
            {
                var item = (JObject)jToken;
                if (item["group"].ToObject<long>() == group && item["qq"].ToObject<long>() == qq)
                {
                    findFlag = true;
                    JObject data = (JObject)item["data"];
                    if (data.ContainsKey(date))
                    {
                        int value = data[date].ToObject<int>() - change;
                        data[date] = value;
                        finalQuota = value;
                    }
                    else
                    {
                        int value = 0 - change;
                        data.Add(date, value);
                        finalQuota = value;
                    }
                }
            }

            if (!findFlag)
            {
                json.Add(new JObject
            {
                new JProperty("group", group),
                new JProperty("qq", qq),
                new JProperty("data", new JObject
                {
                    new JProperty(date, 0 - change)
                })
            });
                finalQuota = 0 - change;
            }
            if (GroupQuotaDict.ContainsKey(group))
            {
                GroupQuotaDict[group] -= change;
            }
            else
            {
                GroupQuotaDict.Add(group, 0 - change);
            }
            File.WriteAllText(path, json.ToString());
            return finalQuota;
        }

        public static void CreateGroupQuotaDict()
        {
            string date = DateTime.Now.ToString("d");
            string path = Path.Combine(MainSave.AppDirectory, "Quota.json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }
            JArray json = JArray.Parse(File.ReadAllText(path));
            foreach (var jToken in json)
            {
                var item = (JObject)jToken;
                JObject data = (JObject)item["data"];
                long group = item["group"].ToObject<long>();
                int quota;
                if (data.ContainsKey(date))
                {
                    quota = data[date].ToObject<int>();
                }
                else
                {
                    quota = 0;
                }
                if (GroupQuotaDict.ContainsKey(group))
                {
                    GroupQuotaDict[group] += quota;
                }
                else
                {
                    GroupQuotaDict.Add(group, quota);
                }
            }
        }
    }
}
