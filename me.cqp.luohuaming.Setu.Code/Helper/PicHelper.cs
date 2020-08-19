using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Threading;
using me.cqp.luohuaming.Setu.Code.Deserializtion;
using System.Text;
using Newtonsoft.Json.Linq;

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
        /// <summary>
        /// 未找到满足关键字的图片
        /// </summary>
        public static string PicNotFound;
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
        /// <summary>
        /// 按pid搜图
        /// </summary>
        public static string PIDSearch;
        /// <summary>
        /// 按关键字搜图
        /// </summary>
        public static string HotSearch;
        /// <summary>
        /// SauceNAO相似度搜索
        /// </summary>
        public static string SauceNaoSearch;
        #endregion

        private static string path, pathUser;
        private static IniConfig ini, iniUser;

        /// <summary>
        /// 判断是否符合取图的条件,若满足,减少1额度
        /// </summary>
        /// <returns></returns>
        public static List<string> JudgeLegality(CQGroupMessageEventArgs e)
        {
            List<string> ls = new List<string>();
            if (e == null || !InGroup(e)) return ls;

            int countofPerson, countofGroup, maxofPerson, maxofGroup;
            countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
            countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
            maxofGroup = ini.Object["Config"]["MaxofGroup"].GetValueOrDefault(30);
            if (countofGroup > maxofGroup)
            {
                if (maxofGroup != 0)
                {
                    ls.Add("-1");
                    ls.Add(MaxGroup);
                    return ls;
                }
            }
            maxofPerson = ini.Object["Config"]["MaxofPerson"].GetValueOrDefault(5);
            if (countofPerson < maxofPerson)
            {
                MinusMemberQuota(e);
                countofPerson++;
            }
            else
            {
                if (maxofPerson != 0)
                {
                    ls.Add("-1");
                    ls.Add(MaxMember);
                    return ls;
                }
                else
                {
                    MinusMemberQuota(e);
                    countofPerson++;
                }
            }
            iniUser.Object["Config"]["Timestamp"] = new IValue(GetTimeStamp());
            iniUser.Save();
            ls.Add("0");
            ls.Add(StartPullPic.Replace("<count>", (maxofPerson - countofPerson).ToString()));
            return ls;
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
            PicNotFound = ini.Object["AnswerDIY"]["PicNotFound"].GetValueOrDefault("未找到满足关键字的图片")
                .Replace(@"\n", "\n");

            LoliConPic = ini.Object["OrderDIY"]["LoliConPic"].GetValueOrDefault("#setu")
                .Replace(@"\n", "\n");
            ClearLimit = ini.Object["OrderDIY"]["ClearLimit"].GetValueOrDefault("#clear")
                .Replace(@"\n", "\n");
            PIDSearch = ini.Object["OrderDIY"]["PIDSearch"].GetValueOrDefault("#pid")
                .Replace(@"\n", "\n");
            HotSearch = ini.Object["OrderDIY"]["HotSearch"].GetValueOrDefault("#搜图")
                .Replace(@"\n", "\n");
            SauceNaoSearch = ini.Object["OrderDIY"]["SauceNao"].GetValueOrDefault("#sauce")
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
                //有两种情况：返回的是json，可以解析；返回的是错误信息，只能手动解析
                Setu deserialize = JsonConvert.DeserializeObject<Setu>(str);
                //计算额度恢复时间
                DateTime dt = DateTime.Now.AddSeconds(deserialize.quota_min_ttl);
                switch (deserialize.code)
                {
                    case 429:
                        e.CQLog.Info("超出额度", "超出额度，次数已归还");
                        PlusMemberQuota(e);
                        OutofQuota = OutofQuota.Replace("<quota_time>", (DateTime.Now.Hour <= dt.Hour) ? $"{dt:HH:mm}" : $"明天 {dt:HH:mm}");
                        return OutofQuota.Replace("<@>", e.FromQQ.CQCode_At().ToSendString()); ;
                    case 404:
                        e.CQLog.Info("图片未找到", "未找到符合要求的图片");
                        PlusMemberQuota(e);
                        return PicNotFound.Replace("<@>", e.FromQQ.CQCode_At().ToSendString()); ;
                    case 401:
                        e.CQLog.Info("APIKEY无效", "APIKEY无效,请更换");
                        PlusMemberQuota(e);
                        return "APIKEY无效，请更换";
                    case 0:
                        string result = Sucess.Replace("<code>", deserialize.code.ToString());
                        result = result.Replace("<@>", e.FromQQ.CQCode_At().ToSendString());
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
                    default://其他情况，返回json本体
                        return str;
                }
            }
            catch//解析失败，手动解析
            {
                try
                {
                    switch (str.Substring(0, 3))//剥离出错误码本身
                    {
                        case "402":
                            e.CQLog.Info("下载错误", "下载错误，次数已归还");
                            PlusMemberQuota(e);
                            return DownloadFailed.Replace("<@>", e.FromQQ.CQCode_At().ToSendString()); ;
                        case "403":
                            e.CQLog.Info("其他错误", str.Substring(3));
                            PlusMemberQuota(e);
                            return ExtraError.Replace("<wrong_msg>", str.Substring(3)).Replace("<@>", e.FromQQ.CQCode_At().ToSendString()); ;
                        default:
                            throw new Exception(str.Substring(0, 3));
                    }
                }
                catch (Exception exc)
                {
                    e.CQLog.Info("解析错误", $"错误信息:{exc.Message}");
                    PlusMemberQuota(e);
                    return $"解析错误，无法继续，错误信息:{exc.Message}".Replace("<@>", e.FromQQ.CQCode_At().ToSendString()); ;
                }
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
        /// 为用户的可用次数减1
        /// </summary>
        /// <param name="e"></param>
        public static void MinusMemberQuota(CQGroupMessageEventArgs e)
        {
            int countofPerson = iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ.Id)].GetValueOrDefault(0);
            int countofGroup = iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"].GetValueOrDefault(0);
            iniUser.Object[$"Count{e.FromGroup.Id}"][string.Format("Count{0}", e.FromQQ)] = new IValue((++countofPerson).ToString());
            iniUser.Object[$"Count{e.FromGroup.Id}"]["CountofGroup"] = new IValue((++countofGroup).ToString());
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

        /// <summary>
        /// 查看指令是否是自定义接口内的
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool CheckCustomAPI(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            if (ls.Count == 0 || string.IsNullOrEmpty(e.Message.Text)) return false;
            foreach (var item in ls)
            {
                if (e.Message.Text == item.Order && item.Enabled)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 自定义接口调用
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        public static void CustomAPI_Call(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            var result = CustomAPI_Image(ls, e);
            QQMessage staues = e.FromGroup.SendGroupMessage(result[1]);
            if (!staues.IsSuccess)//图片发送失败
            {
                //尝试压缩图片
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini"); ini.Load();

                if (ini.Object["Config"]["FailedCompress"].GetValueOrDefault("0") == "0" || !e.FromGroup.SendGroupMessage(CompressImg.CompressImage(CQCode.Parse(result[1])[0].Items["file"])).IsSuccess)
                {
                    e.FromGroup.SendGroupMessage($"图片发送失败");
                    PlusMemberQuota(e);
                    PicHelper.SaveErrorMsg(CQCode.Parse(result[1])[0]);
                }
            }
            if (Convert.ToBoolean(result[0]))//自动撤回
            {
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini"); ini.Load();
                Task task = new Task(() =>
                {
                    Thread.Sleep(ini.Object["R18"]["RevokeTime"] * 1000);
                    RevokePic(staues.Id);
                }); task.Start();
            }
        }

        /// <summary>
        /// 自定义图片拉取
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static List<string> CustomAPI_Image(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            List<string> result = new List<string>();
            List<ItemToSave> order = new List<ItemToSave>();
            foreach (var item in ls)
            {
                if (item.Order == e.Message.Text) order.Add(item);
            }
            try
            {
                //尝试拉取图片，若有多个相同的接口则随机来一个
                ItemToSave item = order[new Random().Next(0, order.Count)];
                result.Add(item.AutoRevoke.ToString());
                //以后要用的路径,先生成一个
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "CustomAPIPic", item.Order);
                if (!Directory.Exists(targetdir))
                {
                    Directory.CreateDirectory(targetdir);
                }
                string imagename = DateTime.Now.ToString("yyyyMMddHHss") + ".jpg";
                string fullpath = Path.Combine(targetdir, imagename);
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Proxy = CQSave.proxy,
                    AllowAutoRedirect = true,
                })
                {
                    http.DownloadFile(item.URL, fullpath);
                }

                e.CQLog.Info("自定义接口", $"图片下载成功，尝试发送");

                GetSetu.AntiHX(fullpath);
                string imagepath = Path.Combine("CustomAPIPic", item.Order, imagename);
                result.Add(CQApi.CQCode_Image(imagepath).ToSendString());
                return result;
            }
            catch (Exception exc)
            {
                result.Add("自定义接口调用失败");
                e.CQLog.Info("自定义接口", $"调用失败，错误信息：{exc.Message}");
                return result;
            }
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="RevokeId">消息ID</param>
        public static void RevokePic(int RevokeId)
        {
            CQSave.cq.RemoveMessage(RevokeId);
        }

        /// <summary>
        /// 查看指令是否是本地图片接口内的
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool CheckLocalPic(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            return CheckCustomAPI(ls, e);
        }

        /// <summary>
        /// 本地图片接口调用
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        public static void LocalPic_Call(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            var result = LocalPic_Image(ls, e);
            e.CQLog.Debug("Debug", result[1]);
            QQMessage staues = e.FromGroup.SendGroupMessage(result[1]);
            if (!staues.IsSuccess)//图片发送失败
            {
                //尝试压缩图片
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini"); ini.Load();
                if (ini.Object["Config"]["FailedCompress"].GetValueOrDefault("0") == "0" || !e.FromGroup.SendGroupMessage(CompressImg.CompressImage(CQCode.Parse(result[1])[0].Items["file"])).IsSuccess)
                {
                    e.FromGroup.SendGroupMessage($"图片发送失败");
                    PlusMemberQuota(e);
                    PicHelper.SaveErrorMsg(result[2]);
                }
            }
            //File.Delete(Path.Combine(CQSave.ImageDirectory, result[2]));
            if (Convert.ToBoolean(result[0]))//自动撤回
            {
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini"); ini.Load();
                Task task = new Task(() =>
                {
                    Thread.Sleep(ini.Object["R18"]["RevokeTime"] * 1000);
                    RevokePic(staues.Id);
                }); task.Start();
            }
        }

        /// <summary>
        /// 本地图片拉取
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static List<string> LocalPic_Image(List<ItemToSave> ls, CQGroupMessageEventArgs e)
        {
            List<string> result = new List<string>();
            ItemToSave item = ls.Where(x => x.Order == e.Message.Text).OrderBy(_ => Guid.NewGuid()).First();
            result.Add(item.AutoRevoke.ToString());
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(item.Path);
                FileInfo[] fileInfos = directoryInfo.GetFiles("*.*")
                    .Where(x => x.FullName.EndsWith("jpg") || x.FullName.EndsWith("gif")
                    || x.FullName.EndsWith("png") || x.FullName.EndsWith("bmp")
                    || x.FullName.EndsWith("webp") || x.FullName.EndsWith("tif") || x.FullName.EndsWith("tga")).ToArray();
                //随机取一个
                var picinfo = fileInfos.OrderBy(_ => Guid.NewGuid()).First();
                string picpathOrigin = picinfo.FullName;
                if (!Directory.Exists(CQSave.ImageDirectory + "\\LocalPic"))
                    Directory.CreateDirectory(CQSave.ImageDirectory + "\\LocalPic");
                string picpathFinal = CQSave.ImageDirectory + "\\LocalPic\\" + picinfo.Name;
                if (!File.Exists(picpathFinal))
                    File.Copy(picpathOrigin, picpathFinal);
                e.CQLog.Info("本地图片接口", $"图片获取成功，尝试发送");
                GetSetu.AntiHX(picpathFinal);
                string imageCQCodePath = Path.Combine("LocalPic", picinfo.Name);
                result.Add(CQApi.CQCode_Image(imageCQCodePath).ToSendString());
                result.Add(imageCQCodePath);
                return result;
            }
            catch (Exception exc)
            {
                result.Add("本地图片接口调用失败");
                e.CQLog.Info("本地图片接口", $"调用失败，错误信息：{exc.Message}");
                if (item.Usestrict)
                    PlusMemberQuota(e);
                return result;
            }
        }

        /// <summary>
        /// SauceNao调用
        /// </summary>
        /// <param name="cqcode">图片CQ码</param>
        /// <param name="e"></param>
        public static void SauceNao_Call(CQCode cqcode, CQGroupMessageEventArgs e)
        {
            string url = "https://saucenao.com/search.php?output_type=2&api_key=56faa0cddf50860330a295e0c331be7c4b4c021f&db=999&numres=1&url=";
            url += GetImageURL(cqcode.ToSendString());
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Proxy = CQSave.proxy,
                AllowAutoRedirect = true,
            })
            {
                try
                {
                    Directory.CreateDirectory(CQSave.ImageDirectory + "SauceNaotemp");
                    var result = JsonConvert.DeserializeObject<SauceNao_Deserial.SauceNAO>(http.DownloadString(url));
                    e.CQLog.Info("SauceNao识图", "结果获取成功，正在拉取缩略图");
                    string str = result.ToString();
                    int count = 1;
                    foreach (var item in result.results)
                    {
                        try
                        {
                            string filename = Guid.NewGuid().ToString().ToString();
                            http.DownloadFile(item.header.thumbnail, $@"{CQSave.ImageDirectory}\SauceNaotemp\{filename}.jpg");
                            str = str.Replace($"{{{count}}}", CQApi.CQCode_Image($@"\SauceNaotemp\{filename}.jpg").ToSendString());
                        }
                        catch
                        {
                            str = str.Replace($"{{{count}}}", item.header.thumbnail);
                        }
                        finally { count++; }
                    }
                    e.FromGroup.SendGroupMessage(str);
                    List<int> ls = result.results.Where(x => x.data.pixiv_id.HasValue).Select(x => x.data.pixiv_id.Value).ToList();
                    if (ls.Count != 0)
                    {
                        e.FromGroup.SendGroupMessage("有Pixiv图片信息，尝试拉取原图...");
                        foreach (var item in ls)
                        {
                            try
                            {
                                if (!File.Exists($@"{CQSave.ImageDirectory}\LoliconPic\${item}.jpg"))
                                {
                                    dynamic jObject = JObject.Parse(Encoding.UTF8.GetString(HttpWebClient.Post("https://api.pixiv.cat/v1/generate", Encoding.UTF8.GetBytes($"p={item}"))));
                                    string pixiv_url = string.Empty;
                                    try
                                    {
                                        var urllist = jObject.original_urls_proxy;
                                        pixiv_url = urllist[0];
                                        e.FromGroup.SendGroupMessage("此图为多P图片，选择第一P下载");
                                        CQSave.cqlog.Info("SauceNao识图", "此图为多P图片，选择第一P下载");
                                    }
                                    catch
                                    {
                                        pixiv_url = jObject.Value<string>("original_url_proxy");
                                    }
                                    http.DownloadFile(pixiv_url, $@"{CQSave.ImageDirectory}\LoliconPic\{item}.jpg");
                                    e.CQLog.Info("SauceNao识图", $"pid={item}的图片下载成功，尝试发送");
                                }
                                QQMessage staues = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image($@"\LoliconPic\{item}.jpg"));
                                if (!staues.IsSuccess)//图片发送失败
                                {
                                    //尝试压缩图片
                                    IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini"); ini.Load();
                                    if (ini.Object["Config"]["FailedCompress"].GetValueOrDefault("0") == "0" || !e.FromGroup.SendGroupMessage(CompressImg.CompressImage($@"\LoliconPic\{item}.jpg")).IsSuccess)
                                    {
                                        e.FromGroup.SendGroupMessage($"pid={item}的图片发送失败");
                                        PicHelper.SaveErrorMsg($@"\LoliconPic\{item}.jpg");
                                    }
                                }
                            }
                            catch (Exception exc)
                            {
                                e.FromGroup.SendGroupMessage($"pid={item}的图片拉取失败,错误信息:{exc.Message}");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    e.CQLog.Info("SauceNao搜图", $"搜索失败，错误信息:{exc.Message}在{exc.StackTrace}");
                    e.FromGroup.SendGroupMessage($"拉取失败，错误信息:{exc.Message}");
                    PlusMemberQuota(e);
                }
                finally
                {
                    Directory.Delete($@"{CQSave.ImageDirectory}\SauceNaotemp", true);
                }
            }
        }

        /// <summary>
        /// 保存错误日志
        /// </summary>
        /// <param name="picpath">CQ码中的file路径</param>
        public static void SaveErrorMsg(string picpath)
        {
            try
            {
                string jsonpath = Path.Combine(CQSave.AppDirectory, "Error", "SendError.json");
                if (!Directory.Exists(CQSave.AppDirectory + "Error"))
                {
                    Directory.CreateDirectory(CQSave.AppDirectory + "Error");
                    File.Create(jsonpath).Dispose();
                    File.WriteAllText(jsonpath, JsonConvert.SerializeObject(new SendError { Msg = new List<ErrorMsg>() }));
                }
                if (!File.Exists(jsonpath))
                {
                    File.Create(jsonpath).Dispose();
                    File.WriteAllText(jsonpath, JsonConvert.SerializeObject(new SendError { Msg = new List<ErrorMsg>() }));
                }
                SendError errorMsg = JsonConvert.DeserializeObject<SendError>(File.ReadAllText(jsonpath));
                errorMsg.Msg.Add(
                    new ErrorMsg()
                    {
                        Date = DateTime.Now.ToString(),
                        FilePath = Path.Combine(CQSave.ImageDirectory, picpath),
                        FileSize = new FileInfo(Path.Combine(CQSave.ImageDirectory, picpath)).Length / 1024 + "KB"
                    });
                File.WriteAllText(jsonpath, JsonConvert.SerializeObject(errorMsg));
                CQSave.cqlog.Info("图片发送失败", "日志已保存");
            }
            catch (Exception e)
            {
                CQSave.cqlog.Info("图片发送失败", $"日志保存失败，错误信息:{e.Message}");
            }
        }

        /// <summary>
        /// 保存错误日志
        /// </summary>
        /// <param name="PicCode">图片CQ码</param>
        public static void SaveErrorMsg(CQCode PicCode)
        {
            SaveErrorMsg(PicCode.Items["file"]);
        }

        /// <summary>
        /// 获取CQ码中的图片网址
        /// </summary>
        /// <param name="imageCQCode">需要解析的图片CQ码</param>
        /// <returns></returns>
        public static string GetImageURL(string imageCQCode)
        {
            string path = CQSave.ImageDirectory + CQCode.Parse(imageCQCode)[0].Items["file"] + ".cqimg";
            IniConfig image = new IniConfig(path);
            image.Load();
            return image.Object["image"]["url"].ToString();
        }
    }
}
