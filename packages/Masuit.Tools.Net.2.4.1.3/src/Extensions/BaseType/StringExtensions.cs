using DnsClient;
using Masuit.Tools.DateTimeExt;
using Masuit.Tools.Strings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Masuit.Tools
{
    public static partial class StringExtensions
    {
        public static string Join(this IEnumerable<string> strs, string separate = ", ") => string.Join(separate, strs);

        /// <summary>
        /// 字符串转时间
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string value)
        {
            DateTime.TryParse(value, out var result);
            return result;
        }

        /// <summary>
        /// 字符串转Guid
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Guid ToGuid(this string s)
        {
            return Guid.Parse(s);
        }

        /// <summary>
        /// 根据正则替换
        /// </summary>
        /// <param name="input"></param>
        /// <param name="regex">正则表达式</param>
        /// <param name="replacement">新内容</param>
        /// <returns></returns>
        public static string Replace(this string input, Regex regex, string replacement)
        {
            return regex.Replace(input, replacement);
        }

        /// <summary>
        /// 生成唯一短字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="chars">可用字符数数量，0-9,a-z,A-Z</param>
        /// <returns></returns>
        public static string CreateShortToken(this string str, byte chars = 36)
        {
            var nf = new NumberFormater(chars);
            return nf.ToString((DateTime.Now.Ticks - 630822816000000000) * 100 + Stopwatch.GetTimestamp() % 100);
        }

        /// <summary>
        /// 任意进制转十进制
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bin">进制</param>
        /// <returns></returns>
        public static long FromBinary(this string str, int bin)
        {
            var nf = new NumberFormater(bin);
            return nf.FromString(str);
        }

        /// <summary>
        /// 任意进制转大数十进制
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bin">进制</param>
        /// <returns></returns>
        public static BigInteger FromBinaryBig(this string str, int bin)
        {
            var nf = new NumberFormater(bin);
            return nf.FromStringBig(str);
        }

        #region 检测字符串中是否包含列表中的关键词

        /// <summary>
        /// 检测字符串中是否包含列表中的关键词
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="keys">关键词列表</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static bool Contains(this string s, IEnumerable<string> keys, bool ignoreCase = true)
        {
            if (!keys.Any() || string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (ignoreCase)
            {
                return Regex.IsMatch(s, string.Join("|", keys.Select(Regex.Escape)), RegexOptions.IgnoreCase);
            }

            return Regex.IsMatch(s, string.Join("|", keys.Select(Regex.Escape)));
        }

        /// <summary>
        /// 判断是否包含符号
        /// </summary>
        /// <param name="str"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public static bool ContainsSymbol(this string str, params string[] symbols)
        {
            return str switch
            {
                null => false,
                string a when string.IsNullOrEmpty(a) => false,
                string a when a == string.Empty => false,
                _ => symbols.Any(t => str.Contains(t))
            };
        }

        #endregion 检测字符串中是否包含列表中的关键词

        /// <summary>
        /// 判断字符串是否为空或""
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// 字符串掩码
        /// </summary>
        /// <param name="s">字符串</param>
        /// <param name="mask">掩码符</param>
        /// <returns></returns>
        public static string Mask(this string s, char mask = '*')
        {
            if (string.IsNullOrWhiteSpace(s?.Trim()))
            {
                return s;
            }
            s = s.Trim();
            string masks = mask.ToString().PadLeft(4, mask);
            return s.Length switch
            {
                >= 11 => Regex.Replace(s, "(.{3}).*(.{4})", $"$1{masks}$2"),
                10 => Regex.Replace(s, "(.{3}).*(.{3})", $"$1{masks}$2"),
                9 => Regex.Replace(s, "(.{2}).*(.{3})", $"$1{masks}$2"),
                8 => Regex.Replace(s, "(.{2}).*(.{2})", $"$1{masks}$2"),
                7 => Regex.Replace(s, "(.{1}).*(.{2})", $"$1{masks}$2"),
                6 => Regex.Replace(s, "(.{1}).*(.{1})", $"$1{masks}$2"),
                _ => Regex.Replace(s, "(.{1}).*", $"$1{masks}")
            };
        }

        #region Email

        /// <summary>
        /// 匹配Email
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="valid">是否验证有效性</param>
        /// <returns>匹配对象；是否匹配成功，若返回true，则会得到一个Match对象，否则为null</returns>
        public static (bool isMatch, Match match) MatchEmail(this string s, bool valid = false)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 7)
            {
                return (false, null);
            }

            Match match = Regex.Match(s, @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
            var isMatch = match.Success;
            if (isMatch && valid)
            {
                var nslookup = new LookupClient();
                var query = nslookup.Query(s.Split('@')[1], QueryType.MX).Answers.MxRecords().SelectMany(r => Dns.GetHostAddresses(r.Exchange.Value)).ToList();
                isMatch = query.Any(ip => !ip.IsPrivateIP());
            }

            return (isMatch, match);
        }

        /// <summary>
        /// 邮箱掩码
        /// </summary>
        /// <param name="s">邮箱</param>
        /// <param name="mask">掩码</param>
        /// <returns></returns>
        public static string MaskEmail(this string s, char mask = '*')
        {
            var index = s.LastIndexOf("@");
            var oldValue = s.Substring(0, index);
            return !MatchEmail(s).isMatch ? s : s.Replace(oldValue, Mask(oldValue, mask));
        }

        #endregion Email

        #region 匹配完整的URL

        /// <summary>
        /// 匹配完整格式的URL
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="isMatch">是否匹配成功，若返回true，则会得到一个Match对象，否则为null</param>
        /// <returns>匹配对象</returns>
        public static Uri MatchUrl(this string s, out bool isMatch)
        {
            try
            {
                var uri = new Uri(s);
                isMatch = Dns.GetHostAddresses(uri.Host).Any(ip => !ip.IsPrivateIP());
                return uri;
            }
            catch
            {
                isMatch = false;
                return null;
            }
        }

        /// <summary>
        /// 匹配完整格式的URL
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <returns>是否匹配成功</returns>
        public static bool MatchUrl(this string s)
        {
            MatchUrl(s, out var isMatch);
            return isMatch;
        }

        #endregion 匹配完整的URL

        #region 权威校验身份证号码

        /// <summary>
        /// 根据GB11643-1999标准权威校验中国身份证号码的合法性
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <returns>是否匹配成功</returns>
        public static bool MatchIdentifyCard(this string s)
        {
            return s.Length switch
            {
                18 => CheckChinaID18(s),
                15 => CheckChinaID15(s),
                _ => false
            };
        }

        private static readonly string[] ChinaIDProvinceCodes = {
             "11", "12", "13", "14", "15",
             "21", "22", "23",
             "31", "32", "33", "34", "35", "36", "37",
             "41", "42", "43", "44", "45", "46",
             "50", "51", "52", "53", "54",
             "61", "62", "63", "64", "65",
             "71",
             "81", "82",
             "91"
        };

        private static bool CheckChinaID18(string ID)
        {
            ID = ID.ToUpper();
            Match m = Regex.Match(ID, @"\d{17}[\dX]", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return false;
            }
            if (!ChinaIDProvinceCodes.Contains(ID.Substring(0, 2)))
            {
                return false;
            }
            CultureInfo zhCN = new CultureInfo("zh-CN", true);
            if (!DateTime.TryParseExact(ID.Substring(6, 8), "yyyyMMdd", zhCN, DateTimeStyles.None, out DateTime birthday))
            {
                return false;
            }
            if (!birthday.In(new DateTime(1800, 1, 1), DateTime.Today))
            {
                return false;
            }
            int[] factors = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                sum += (ID[i] - '0') * factors[i];
            }
            int n = (12 - sum % 11) % 11;
            return n < 10 ? ID[17] - '0' == n : ID[17].Equals('X');
        }

        private static bool CheckChinaID15(string ID)
        {
            Match m = Regex.Match(ID, @"\d{15}", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return false;
            }
            if (!ChinaIDProvinceCodes.Contains(ID.Substring(0, 2)))
            {
                return false;
            }
            CultureInfo zhCN = new CultureInfo("zh-CN", true);
            if (!DateTime.TryParseExact("19" + ID.Substring(6, 6), "yyyyMMdd", zhCN, DateTimeStyles.None, out DateTime birthday))
            {
                return false;
            }
            return birthday.In(new DateTime(1800, 1, 1), new DateTime(2000, 1, 1));
        }

        #endregion 权威校验身份证号码

        #region IP地址

        /// <summary>
        /// 校验IP地址的正确性，同时支持IPv4和IPv6
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="isMatch">是否匹配成功，若返回true，则会得到一个Match对象，否则为null</param>
        /// <returns>匹配对象</returns>
        public static IPAddress MatchInetAddress(this string s, out bool isMatch)
        {
            isMatch = IPAddress.TryParse(s, out var ip);
            return ip;
        }

        /// <summary>
        /// 校验IP地址的正确性，同时支持IPv4和IPv6
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <returns>是否匹配成功</returns>
        public static bool MatchInetAddress(this string s)
        {
            MatchInetAddress(s, out var success);
            return success;
        }

        /// <summary>
        /// IP地址转换成数字
        /// </summary>
        /// <param name="addr">IP地址</param>
        /// <returns>数字,输入无效IP地址返回0</returns>
        public static uint IPToID(this string addr)
        {
            if (!IPAddress.TryParse(addr, out var ip))
            {
                return 0;
            }

            byte[] bInt = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bInt);
            }

            return BitConverter.ToUInt32(bInt, 0);
        }

        /// <summary>
        /// 判断IP是否是私有地址
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsPrivateIP(this string ip)
        {
            if (MatchInetAddress(ip))
            {
                return IPAddress.Parse(ip).IsPrivateIP();
            }

            return false;
        }

        /// <summary>
        /// 判断IP地址在不在某个IP地址段
        /// </summary>
        /// <param name="input">需要判断的IP地址</param>
        /// <param name="begin">起始地址</param>
        /// <param name="ends">结束地址</param>
        /// <returns></returns>
        public static bool IpAddressInRange(this string input, string begin, string ends)
        {
            uint current = input.IPToID();
            return current >= begin.IPToID() && current <= ends.IPToID();
        }

        #endregion IP地址

        #region 校验手机号码的正确性

        /// <summary>
        /// 匹配手机号码
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="isMatch">是否匹配成功，若返回true，则会得到一个Match对象，否则为null</param>
        /// <returns>匹配对象</returns>
        public static Match MatchPhoneNumber(this string s, out bool isMatch)
        {
            if (string.IsNullOrEmpty(s))
            {
                isMatch = false;
                return null;
            }
            Match match = Regex.Match(s, @"^((1[3,5,6,8][0-9])|(14[5,7])|(17[0,1,3,6,7,8])|(19[8,9]))\d{8}$");
            isMatch = match.Success;
            return isMatch ? match : null;
        }

        /// <summary>
        /// 匹配手机号码
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <returns>是否匹配成功</returns>
        public static bool MatchPhoneNumber(this string s)
        {
            MatchPhoneNumber(s, out bool success);
            return success;
        }

        #endregion 校验手机号码的正确性

        #region Url

        /// <summary>
        /// 判断url是否是外部地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsExternalAddress(this string url)
        {
            var uri = new Uri(url);
            switch (uri.HostNameType)
            {
                case UriHostNameType.Dns:
                    var ipHostEntry = Dns.GetHostEntry(uri.DnsSafeHost);
                    if (ipHostEntry.AddressList.Where(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork).Any(ipAddress => !ipAddress.IsPrivateIP()))
                    {
                        return true;
                    }
                    break;

                case UriHostNameType.IPv4:
                    return !IPAddress.Parse(uri.DnsSafeHost).IsPrivateIP();
            }
            return false;
        }

        #endregion Url

        /// <summary>
        /// 转换成字节数组
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this string @this)
        {
            return Encoding.ASCII.GetBytes(@this);
        }

        #region Crc32

        /// <summary>
        /// 获取字符串crc32签名
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Crc32(this string s)
        {
            return string.Join(string.Empty, new Security.Crc32().ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2")));
        }

        /// <summary>
        /// 获取字符串crc64签名
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Crc64(this string s)
        {
            return string.Join(string.Empty, new Security.Crc64().ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2")));
        }

        #endregion Crc32

        #region 权威校验中国专利申请号/专利号
        /// <summary>
        /// 中国专利申请号（授权以后就是专利号）由两种组成
        /// 2003年9月30号以前的9位（不带校验位是8号），校验位之前可能还会有一个点，例如：00262311, 002623110 或 00262311.0
        /// 2003年10月1号以后的13位（不带校验位是12号），校验位之前可能还会有一个点，例如：200410018477, 2004100184779 或200410018477.9
        /// http://www.sipo.gov.cn/docs/pub/old/wxfw/zlwxxxggfw/hlwzljsxt/hlwzljsxtsyzn/201507/P020150713610193194682.pdf
        /// 上面的文档中均不包括校验算法，但是下面的校验算法没有问题
        /// </summary>
        /// <param name="patnum">源字符串</param>
        /// <returns>是否匹配成功</returns>
        public static bool MatchCNPatentNumber(this string patnum)
        {
            Regex patnumWithCheckbitPattern = new Regex(@"^
(?<!\d)
(?<patentnum>
    (?<basenum>
        (?<year>(?<old>8[5-9]|9[0-9]|0[0-3])|(?<new>[2-9]\d{3}))
        (?<sn>
            (?<patenttype>[12389])
            (?(old)\d{5}|(?(new)\d{7}))
        )
    )
    (?:
    \.?
    (?<checkbit>[0-9X])
    )?
)
(?!\d)
$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Match m = patnumWithCheckbitPattern.Match(patnum);
            if (!m.Success)
            {
                return false;
            }
            bool isPatnumTrue = true;
            patnum = patnum.ToUpper().Replace(".", "");
            if (patnum.Length == 9 || patnum.Length == 8)
            {
                byte[] factors8 = new byte[8] { 2, 3, 4, 5, 6, 7, 8, 9 };
                int year = Convert.ToUInt16(patnum.Substring(0, 2));
                year += (year >= 85) ? (ushort)1900u : (ushort)2000u;
                if (year >= 1985 || year <= 2003)
                {
                    int sum = 0;
                    for (byte i = 0; i < 8; i++)
                    {
                        sum += factors8[i] * (patnum[i] - '0');
                    }
                    char checkbit = "0123456789X"[sum % 11];
                    if (patnum.Length == 9)
                    {
                        if (checkbit != patnum[8])
                        {
                            isPatnumTrue = false;
                        }
                    }
                    else
                    {
                        patnum += checkbit;
                    }
                }
                else
                {
                    isPatnumTrue = false;
                }
            }
            else if (patnum.Length == 13 || patnum.Length == 12)
            {
                byte[] factors12 = new byte[12] { 2, 3, 4, 5, 6, 7, 8, 9, 2, 3, 4, 5 };
                int year = Convert.ToUInt16(patnum.Substring(0, 4));
                if (year >= 2003 && year <= DateTime.Now.Year)
                {
                    int sum = 0;
                    for (byte i = 0; i < 12; i++)
                    {
                        sum += factors12[i] * (patnum[i] - '0');
                    }
                    char checkbit = "0123456789X"[sum % 11];
                    if (patnum.Length == 13)
                    {
                        if (checkbit != patnum[12])
                        {
                            isPatnumTrue = false;
                        }
                    }
                    else
                    {
                        patnum += checkbit;
                    }
                }
                else
                {
                    isPatnumTrue = false;
                }
            }
            else
            {
                isPatnumTrue = false;
            }
            return isPatnumTrue;
        }
    }
    #endregion
}