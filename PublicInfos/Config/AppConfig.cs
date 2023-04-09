using System.Collections.Generic;

namespace me.cqp.luohuaming.Setu.PublicInfos.Config
{
    public enum AntiBanType
    {
        Filp,
        MD5
    }

    public static class AppConfig
    {
        public static int MaxGroupQuota { get; set; }
        public static int MaxPersonQuota { get; set; }
        public static bool R18 { get; set; } = false;
        public static bool R18_PicRevoke { get; set; }
        public static int R18_RevokeTime { get; set; }
        public static bool AntiBan { get; set; }
        public static AntiBanType AntiBanType { get; set; }
        public static bool ProxyEnabled { get; set; }
        public static string ProxyURL { get; set; }
        public static string ProxyUserName { get; set; }
        public static string ProxyPassword { get; set; }
        public static bool PassSNI { get; set; }
        public static string SNI_IPAddress { get; set; }
        public static string PixivCookie { get; set; }
        public static string PixivUA { get; set; }

        /// <summary>
        /// 开始拉取图片
        /// </summary>
        public static string StartResponse { get; set; }
        /// <summary>
        /// 接口额度达到上限
        /// </summary>
        public static string OutofQuota { get; set; }
        /// <summary>
        /// 个人调用达到上限
        /// </summary>
        public static string MaxMemberResoponse { get; set; }
        /// <summary>
        /// 群调用达到上限
        /// </summary>
        public static string MaxGroupResoponse { get; set; }
        /// <summary>
        /// 成功拉取图片
        /// </summary>
        public static string SuccessResponse { get; set; }
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
        public static string PicNotFoundResoponse { get; set; }
        public static List<long> GroupList { get; set; }
        public static List<long> AdminList { get; set; }
    }
}
