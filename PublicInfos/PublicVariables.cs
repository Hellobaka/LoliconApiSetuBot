using Native.Tool.IniConfig;

namespace PublicInfos
{
    public static class PublicVariables
    {
        #region --返回文本--
        /// <summary>
        /// 开始拉取图片
        /// </summary>
        public static string StartPullPic { get; set; }
        /// <summary>
        /// 接口额度达到上限
        /// </summary>
        public static string OutofQuota { get; set; }
        /// <summary>
        /// 下载图片失败
        /// </summary>
        public static string DownloadFailed { get; set; }
        /// <summary>
        /// 个人调用达到上限
        /// </summary>
        public static string MaxMember { get; set; }
        /// <summary>
        /// 群调用达到上限
        /// </summary>
        public static string MaxGroup { get; set; }
        /// <summary>
        /// 成功拉取图片
        /// </summary>
        public static string Sucess { get; set; }
        /// <summary>
        /// 其他错误
        /// </summary>
        public static string ExtraError { get; set; }
        /// <summary>
        /// 图片发送失败
        /// </summary>
        public static string SendPicFailed { get; set; }
        /// <summary>
        /// 未找到满足关键字的图片
        /// </summary>
        public static string PicNotFound { get; set; }
        #endregion

        #region --指令文本--
        /// <summary>
        /// 调用LoliCon接口
        /// </summary>
        public static string LoliConPic { get; set; }
        /// <summary>
        /// 清除限制
        /// </summary>
        public static string ClearLimit { get; set; }
        /// <summary>
        /// 按pid搜图
        /// </summary>
        public static string PIDSearch { get; set; }
        /// <summary>
        /// 按关键字搜图
        /// </summary>
        public static string HotSearch { get; set; }
        /// <summary>
        /// SauceNAO相似度搜索
        /// </summary>
        public static string SauceNaoSearch { get; set; }
        #endregion

        public static string Lolicon_ApiKey { get; set; }
        public static bool Lolicon_ApiSwitch { get; set; } = false;
        public static int MaxNumofGroup { get; set; }
        public static int MaxNumofPerson { get; set; }
        public static string PixivicAuth { get; set; }
        public static bool R18_Flag { get; set; } = false;
        public static bool R18_PicRevoke { get; set; } = false;
        public static int R18_RevokeTime { get; set; }


        /// <summary>
        /// 读取返回自定义指令与回答内容
        /// </summary>
        public static void ReadOrderandAnswer()
        {
            IniConfig ini = MainSave.ConfigMain;

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
            SauceNaoSearch = ini.Object["OrderDIY"]["SauceNao"].GetValueOrDefault("#nao")
                .Replace(@"\n", "\n");

            Lolicon_ApiKey = ini.Object["Config"]["ApiKey"].GetValueOrDefault("");
            Lolicon_ApiSwitch = ini.Object["Config"]["ApiSwitch"].GetValueOrDefault(0)==1;
            MaxNumofGroup = ini.Object["Config"]["MaxofGroup"].GetValueOrDefault(0);
            MaxNumofPerson = ini.Object["Config"]["MaxofPerson"].GetValueOrDefault(10);
            PixivicAuth = ini.Object["Config"]["PixivicAuth"].GetValueOrDefault("");
            R18_Flag = ini.Object["R18"]["Enabled"].GetValueOrDefault(0) == 1;
            R18_PicRevoke = ini.Object["R18"]["R18PicRevoke"].GetValueOrDefault(0)==1;
            R18_RevokeTime = ini.Object["R18"]["RevokeTime"].GetValueOrDefault(20);
        }
    }
}
