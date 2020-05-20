using Native.Sdk.Cqp.EventArgs;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace me.cqp.luohuaming.Setu.Code
{
    public static class PicHelper
    {
        #region --返回文本--
        /// <summary>
        /// 开始拉取图片
        /// </summary>
        public static string StartPullPic;
        /// <summary>
        /// 接口额度达到上限
        /// </summary>
        public static string OutofQuota;
        /// <summary>
        /// 下载图片失败
        /// </summary>
        public static string DownloadFailed;
        /// <summary>
        /// 个人调用达到上限
        /// </summary>
        public static string MaxMember;
        /// <summary>
        /// 群调用达到上限
        /// </summary>
        public static string MaxGroup;
        /// <summary>
        /// 成功拉取图片
        /// </summary>
        public static string Sucess;
        /// <summary>
        /// 其他错误
        /// </summary>
        public static string ExtraError;
        /// <summary>
        /// 图片发送失败
        /// </summary>
        public static string SendPicFailed;
        #endregion

        #region --指令文本--
        /// <summary>
        /// 调用LoliCon接口
        /// </summary>
        public static string LoliConPic;
        /// <summary>
        /// 清除限制
        /// </summary>
        public static string ClearLimit;
        #endregion

        private static string path, pathUser;
        private static IniConfig ini, iniUser;

        /// <summary>
        /// 判断是否符合取图的条件
        /// </summary>
        /// <returns></returns>
        public static string JudgeLegality(CQGroupMessageEventArgs e)
        {
            if (e == null || !InGroup(e)) return string.Empty;

            int countofPerson, countofGroup, maxofPerson, maxofGroup;
            countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
            countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
            maxofGroup = ini.Object["Config"]["MaxofGroup"].GetValueOrDefault(30);
            if (countofGroup > maxofGroup)
            {
                if (maxofGroup != 0) return MaxGroup;
            }
            maxofPerson = ini.Object["Config"]["MaxofPerson"].GetValueOrDefault(5);
            if (countofPerson < maxofPerson)
            {
                iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ)] = new IValue((++countofPerson).ToString());
                iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"] = new IValue((++countofGroup).ToString());
            }
            else
            {
                if (maxofPerson != 0) { return MaxMember; }
                else
                {
                    iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ)] = new IValue((++countofPerson).ToString());
                    iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"] = new IValue((++countofGroup).ToString());
                }
            }
            iniUser.Object["Config"]["Timestamp"] = new IValue(GetTimeStamp());
            iniUser.Save();
            return StartPullPic.Replace("<count>", (maxofPerson - countofPerson).ToString());
        }

        /// <summary>
        /// 读取返回自定义指令与回答内容
        /// </summary>
        public static void ReadOrderandAnswer()
        {
            path = CQSave.AppDirectory + "\\Config.ini";
            pathUser = CQSave.AppDirectory + "\\ConfigLimit.ini";
            ini = new IniConfig(path); iniUser = new IniConfig(pathUser);
            ini.Load(); iniUser.Load();

            Sucess = ini.Object["AnswerDIY"]["Sucess"].GetValueOrDefault("机器人当日剩余调用次数:<quota>\n下次额度恢复时间为:<quota_time>\ntitle: <title>\nauthor: <author>\np: <p>\npid: <pid>")
                .Replace(@"\n", "\n");
            StartPullPic = ini.Object["AnswerDIY"]["StartPullPic"].GetValueOrDefault("拉取图片中~至少需要15s……\n你今日剩余调用次数为<count>次(￣▽￣)")
                .Replace(@"\n", "\n");
            OutofQuota = ini.Object["AnswerDIY"]["OutofQuota"].GetValueOrDefault("接口额度达到上限，请等待接口额度回复\n下次额度恢复的时间是:<quota_time>")
                .Replace(@"\n", "\n");
            DownloadFailed = ini.Object["AnswerDIY"]["DownloadFailed"].GetValueOrDefault("图片下载失败，次数已归还")
                .Replace(@"\n", "\n");
            SendPicFailed = ini.Object["AnswerDIY"]["SendPicFailed"].GetValueOrDefault("由于不可抗力导致图被吞，复制进浏览器看看吧:<url>")
                .Replace(@"\n", "\n");
            MaxMember = ini.Object["AnswerDIY"]["MaxMember"].GetValueOrDefault("你当日所能调用的次数已达上限(￣▽￣)")
                .Replace(@"\n", "\n");
            MaxGroup = ini.Object["AnswerDIY"]["MaxGroup"].GetValueOrDefault("本群当日所能调用的次数已达上限(￣▽￣)")
                .Replace(@"\n", "\n");
            ExtraError = ini.Object["AnswerDIY"]["ExtraError"].GetValueOrDefault("发生错误，请尝试重新调用，错误信息:<wrong_msg>")
                .Replace(@"\n", "\n");

            LoliConPic = ini.Object["OrderDIY"]["LoliConPic"].GetValueOrDefault("#setu")
                .Replace(@"\n", "\n");
            ClearLimit = ini.Object["OrderDIY"]["ClearLimit"].GetValueOrDefault("#clear")
                .Replace(@"\n", "\n");
        }

        /// <summary>
        /// 处理返回文本，替换可配置文本为结果
        /// </summary>
        /// <param name="str">待处理文本</param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string ProcessReturns(string str, CQGroupMessageEventArgs e)
        {
            //<code> 返回码
            //<msg> 错误信息之类的
            //<quota> 剩余调用额度
            //<quota_min_ttl> 距离下一次调用额度恢复(+1)的秒数
            //<quota_time> 下次额度回复的DataTime表示
            //<pid> 作品 PID
            //<p> 作品所在 P
            //<uid> 作者 UID
            //<title> 作品标题
            //<author> 作者名（入库时，并过滤掉 @ 及其后内容）
            //<url> 图片链接（可能存在有些作品因修改或删除而导致 404 的情况）
            //<r18> 是否 R18（在色图库中的分类，并非作者标识的 R18）
            //<width> 原图宽度 px
            //<height> 原图高度 px
            //<#> 自定义变量
            if (e == null || str == null) return string.Empty;
            int countofPerson, countofGroup;
            try
            {
                if (str.StartsWith("403")) throw new Exception();
                Setu deserialize = JsonConvert.DeserializeObject<Setu>(str);
                DateTime dt = DateTime.Now.AddSeconds(deserialize.quota_min_ttl);
                switch (str)
                {
                    case "401":
                        e.CQLog.Info("超出额度", "超出额度，次数已归还");
                        PlusMemberQuota(e);
                        OutofQuota = OutofQuota.Replace("<quota_time>", (DateTime.Now.Hour <= dt.Hour) ? $"{dt:HH:mm}" : $"明天 {dt:HH:mm}");
                        return OutofQuota;
                    case "402":
                        e.CQLog.Info("下载错误", "下载错误，次数已归还");
                        PlusMemberQuota(e);
                        return DownloadFailed;
                    default:
                        if (str.StartsWith("403", StringComparison.OrdinalIgnoreCase))
                        {
                            PlusMemberQuota(e);
                            return ExtraError.Replace("<wrong_msg>", str.Substring("403".Length));
                        }
                        else
                        {
                            string result = Sucess.Replace("<code>", deserialize.code.ToString());
                            result = result.Replace("<msg>", deserialize.msg);
                            result = result.Replace("<quota>", deserialize.quota.ToString());
                            result = result.Replace("<quota_min_ttl>", deserialize.quota_min_ttl.ToString());
                            result = result.Replace("<quota_time>", (DateTime.Now.Hour <= dt.Hour) ? $"{dt:HH:mm}" : $"明天 {dt:HH:mm}");
                            List<Data> pic = deserialize.data;
                            Data picinfo = pic[0];
                            result = result.Replace("<pid>", picinfo.pid);
                            result = result.Replace("<p>", picinfo.p.ToString());
                            result = result.Replace("<uid>", picinfo.uid);
                            result = result.Replace("<title>", picinfo.title);
                            result = result.Replace("<author>", picinfo.author);
                            result = result.Replace("<url>", picinfo.url);
                            result = result.Replace("<r18>", picinfo.r18);
                            result = result.Replace("<width> ", picinfo.width);
                            result = result.Replace("<height>", picinfo.height);
                            return result;

                        }
                }
            }
            catch (Exception exc)
            {
                return $"解析错误，无法继续，错误信息:{exc.Message}";
            }

        }

        /// <summary>
        /// 为用户的可用次数加1
        /// </summary>
        /// <param name="e"></param>
        /// <param name="countofPerson"></param>
        /// <param name="countofGroup"></param>
        public static void PlusMemberQuota(CQGroupMessageEventArgs e)
        {
            int countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
            int countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
            iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ)] = new IValue((--countofPerson).ToString());
            iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"] = new IValue((--countofGroup).ToString());
            iniUser.Save();
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// 判断请求群是否存在于配置中
        /// </summary>
        /// <returns></returns>
        public static bool InGroup(CQGroupMessageEventArgs e)
        {
            int count = ini.Object["GroupList"]["Count"].GetValueOrDefault(0);
            for (int i = 0; i < count; i++)
            {
                if (e.FromGroup.Id == ini.Object["GroupList"][$"Index{i}"].GetValueOrDefault((long)0))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断调用者是否为插件管理员
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsAdmin(CQGroupMessageEventArgs e)
        {
            int count = ini.Object["Admin"]["Count"].GetValueOrDefault(0);
            for (int i = 0; i < count; i++)
            {
                if (ini.Object["Admin"][$"Index{i}"].GetValueOrDefault((long)0) == e.FromQQ.Id)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
