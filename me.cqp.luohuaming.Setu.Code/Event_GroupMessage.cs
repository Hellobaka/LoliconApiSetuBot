using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json;

namespace me.cqp.luohuaming.Setu.Code
{
    //INIhelper\.IniRead\((\S+), (\S+), (\S+), \S+?\)
    //INIhelper\.IniWrite\((\S+), (\S+), (\S+), \S+?\)
    //ini.Object[$1][$2].GetValueOrDefault($3)
    //ini.Object[$1][$2]=new IValue($3)
    public class Event_GroupMessage : IGroupMessage
    {
        #region --返回文本--
        /// <summary>
        /// 开始拉取图片
        /// </summary>
        private string StartPullPic;
        /// <summary>
        /// 接口额度达到上限
        /// </summary>
        private string NonQuota;
        /// <summary>
        /// 下载图片失败
        /// </summary>
        private string FailedDownloadPic;
        /// <summary>
        /// 个人调用达到上限
        /// </summary>
        private string MaxMember;
        /// <summary>
        /// 群调用达到上限
        /// </summary>
        private string MaxGroup;
        /// <summary>
        /// 成功拉取图片
        /// </summary>
        private string SuccessPullPic;
        /// <summary>
        /// 其他错误
        /// </summary>
        private string ExtraError;
        #endregion

        public static string path;
        public static string pathUser;

        CQGroupMessageEventArgs cq;
        public IniConfig ini, iniUser;
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {

            path = CQSave.AppDirectory + "\\Config.ini";
            pathUser = CQSave.AppDirectory + "\\ConfigLimit.ini";
            cq = e;
            ini = new IniConfig(path); iniUser = new IniConfig(pathUser);
            ini.Load(); iniUser.Load();
            if (e.Message.Text.Replace("＃","#") == "#setu")
            {
                ReadResponseText();
                string response = JudgeLegality();
                if (response == "-401") return;
                e.Handler = true;
                e.CQApi.SendGroupMessage(e.FromGroup, response);
                if (response == MaxGroup || response == MaxMember) return;
                GetSetu setu = new GetSetu();//处理时间受地区与网速限制
                List<string> pic = setu.GetSetuPic();
                try
                {
                    List<string> save = pic;
                    string str = ProcessReturns(pic[0], e);
                    int quota_second = 0;
                    if (int.TryParse(pic[1], out quota_second))
                    {
                        e.CQApi.SendGroupMessage(e.FromGroup, str + $" 下次额度恢复的时间是:{DateTime.Now.AddSeconds(quota_second):HH:mm}");
                    }
                    else
                    {
                        e.CQApi.SendGroupMessage(e.FromGroup, str);
                    }
                    if (File.Exists(CQSave.ImageDirectory + $"\\{pic[1]}"))
                    {
                        QQMessage staues = e.CQApi.SendGroupMessage(e.FromGroup, CQApi.CQCode_Image(pic[1]));
                        if (!staues.IsSuccess)
                        {
                            Setu deserialize = JsonConvert.DeserializeObject<Setu>(pic[0]);
                            List<Data> msg = deserialize.data;
                            e.CQApi.SendGroupMessage(e.FromGroup, $"由于不可抗力导致图被吞，复制进浏览器看看吧:{msg[0].url}");
                        }
                    }
                }
                catch (Exception exc)
                {
                    e.CQApi.SendGroupMessage(e.FromGroup, $"发生未知错误,错误信息:在{exc.Source}上, 发送错误: {exc.Message}  有{exc.StackTrace}");
                }
            }
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
        /// 判断是否符合取图的条件
        /// </summary>
        /// <returns></returns>
        string JudgeLegality()
        {
            int count = ini.Object["GroupList"]["Count"].GetValueOrDefault(0);
            bool flag = false;
            for (int i = 0; i < count; i++)
            {
                if (cq.FromGroup.Id == ini.Object["GroupList"][$"Index{i}"].GetValueOrDefault((long)0))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag) return "-401";
            int countofPerson, countofGroup, maxofPerson, maxofGroup;
            countofPerson = iniUser.Object[$"Count{cq.FromGroup.Id}"][string.Format("Count{0}", cq.FromQQ.Id)].GetValueOrDefault(0);
            countofGroup = iniUser.Object[$"Count{cq.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
            maxofGroup = ini.Object["Config"]["MaxofGroup"].GetValueOrDefault(30);
            if (countofGroup > maxofGroup)
            {
                if (maxofGroup != 0) return MaxGroup;
            }
            maxofPerson = ini.Object["Config"]["MaxofPerson"].GetValueOrDefault(5);
            if (countofPerson < maxofPerson)
            {
                iniUser.Object[$"Count{cq.FromGroup.Id}"][string.Format("Count{0}",cq.FromQQ)]=new IValue((++countofPerson).ToString());
                iniUser.Object[$"Count{cq.FromGroup.Id}"]["CountofGroup"]=new IValue((++countofGroup).ToString());
            }
            else
            {
                if (maxofPerson != 0) { return MaxMember; }
                else
                {
                    iniUser.Object[$"Count{cq.FromGroup.Id}"][string.Format("Count{0}",cq.FromQQ)]=new IValue((++countofPerson).ToString());
                    iniUser.Object[$"Count{cq.FromGroup.Id}"]["CountofGroup"]=new IValue((++countofGroup).ToString());
                }
            }
            iniUser.Object["Config"]["Timestamp"]=new IValue(GetTimeStamp());
            iniUser.Save();
            return StartPullPic.Replace("<#>", (maxofPerson - countofPerson).ToString());
        }
        /// <summary>
        /// 读取返回文本内容
        /// </summary>
        void ReadResponseText()
        {
            SuccessPullPic = ini.Object["Text"]["MaxGroup"].GetValueOrDefault("机器人当日剩余调用次数:<quota>\n下次额度恢复时间为:<quota_time>\ntitle: <title>\nauthor: <author>\np: <p>\npid: <pid>");
            StartPullPic = ini.Object["Text"]["StartPullPic"].GetValueOrDefault("拉取图片中~至少需要15s……\n你今日剩余调用次数为<#>次(￣▽￣)");
            NonQuota = ini.Object["Text"]["NonQuota"].GetValueOrDefault("接口额度达到上限，请等待接口额度回复");
            FailedDownloadPic = ini.Object["Text"]["FailedDownloadPic"].GetValueOrDefault("图片下载失败，次数已归还");
            MaxMember = ini.Object["Text"]["MaxMember"].GetValueOrDefault("你当日所能调用的次数已达上限(￣▽￣)");
            MaxGroup = ini.Object["Text"]["MaxGroup"].GetValueOrDefault("本群当日所能调用的次数已达上限(￣▽￣)");
            ExtraError = ini.Object["Text"]["ExtraError"].GetValueOrDefault("发生错误，请尝试重新调用，错误信息:<#>");
        }

        string ProcessReturns(string str, CQGroupMessageEventArgs e)
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
            int countofPerson, countofGroup;
            switch (str)
            {
                case "401":
                    cq.CQLog.Info("超出额度", "超出额度，次数已归还");
                    countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
                    countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
                    iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}",e.FromQQ)]=new IValue((--countofPerson).ToString());
                    iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"]=new IValue((--countofGroup).ToString());
                    iniUser.Save();
                    return NonQuota;
                case "402":
                    cq.CQLog.Info("下载错误", "下载错误，次数已归还");
                    countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
                    countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
                    iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}",e.FromQQ)]=new IValue((--countofPerson).ToString());
                    iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"]=new IValue((--countofGroup).ToString());
                    iniUser.Save();
                    return FailedDownloadPic;
                default:
                    if (str.StartsWith("403", StringComparison.OrdinalIgnoreCase))
                    {
                        countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
                        countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
                        iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}",e.FromQQ)]=new IValue((--countofPerson).ToString());
                        iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"]=new IValue((--countofGroup).ToString());
                        iniUser.Save();
                        return ExtraError.Replace("<#>", str.Substring("403".Length));
                    }
                    else
                    {
                        try
                        {
                            Setu deserialize = JsonConvert.DeserializeObject<Setu>(str);
                            string result = SuccessPullPic.Replace("<code>", deserialize.code.ToString());
                            result = result.Replace("<msg>", deserialize.msg);
                            result = result.Replace("<quota>", deserialize.quota.ToString());
                            result = result.Replace("<quota_min_ttl>", deserialize.quota_min_ttl.ToString());
                            DateTime dt = DateTime.Now.AddSeconds(deserialize.quota_min_ttl);
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
                        catch (Exception exc)
                        {
                            return $"解析错误，无法继续，错误信息:{exc.Message}";
                        }
                    }
            }            
        }
    }
}
