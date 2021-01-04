using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using me.cqp.luohuaming.Setu.Code.Deserializtion;
using me.cqp.luohuaming.Setu.Code.Deserializtion.HotSearch;
using me.cqp.luohuaming.Setu.Code.Deserializtion.PixivIllust;
using me.cqp.luohuaming.Setu.Code.Deserializtion.PixivRank;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.Code
{
    #region --说明--
    //人气搜索使用以下接口，很棒
    //https://api.pixivic.com/illustrations?illustType=illust&searchType=original&maxSanityLevel=6&page=1&keyword={关键字}&pageSize=30
    //预览图使用pixiv反代，使用缩略图
    //图片详情、排行榜使用pixiv_api https://api.imjad.cn/pixiv_v2.md
    #endregion
    public class IllustInfo
    {
        public string IllustText { get; set; }
        public CQCode IllustCQCode { get; set; }
        public string IllustUrl { get; set; }
        public bool R18_Flag { get; set; } = false;
    }
    public class PixivAPI
    {
        /// <summary>
        /// 获取排行榜
        /// </summary>
        /// <param name="mode">指定排行榜的类型 可选参数：day-日榜 week-周榜 month-月榜...详见https://api.imjad.cn/pixiv_v2.md</param>
        /// <param name="page">页数，从1开始，默认为1</param>
        /// <returns></returns>
        public static string GetRank(string mode, int page = 1)
        {
            string url = $"https://api.imjad.cn/pixiv/v2/?type=rank&mode={mode}&page={page}";
            string returnstr = Encoding.UTF8.GetString(HttpWebClient.Get(url));
            Pixiv_Rank rank = JsonConvert.DeserializeObject<Pixiv_Rank>(returnstr);

            return $"[CQ:at,file=Empty.png]";
        }
        /// <summary>
        /// 获取图片详情及原图
        /// </summary>
        /// <param name="id">图片的Pid</param>
        /// <returns></returns>
        public static IllustInfo GetIllustInfo(int id)
        {
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                string url = $"https://pix.ipv4.host/illusts/{id}";
                string returnstr = string.Empty;
                try
                {
                    returnstr = http.DownloadString(url);
                    Pixiv_PID infobase = JsonConvert.DeserializeObject<Pixiv_PID>(returnstr);
                    bool r18_Flag = infobase.data.tags.Any(x => x.name.Contains("R-18"));
                    if (r18_Flag && !PublicVariables.R18_Flag)
                    {
                        IllustInfo R18Pic = new IllustInfo()
                        {
                            IllustText = "设置内限制级图片，不予显示",
                            IllustCQCode = new CQCode(CQFunction.Image, new KeyValuePair<string, string>("file", "Error.jpg"))
                        };
                        return R18Pic;
                    }
                    IllustInfo illustInfo = new IllustInfo()
                    {
                        IllustText = Pixiv_Illust.GetIllustReturnText(infobase),
                        IllustCQCode = Pixiv_Illust.GetIllustPic(infobase),
                        R18_Flag = r18_Flag
                    };
                    illustInfo.IllustUrl = infobase.data.imageUrls[0].original.Replace("pximg.net", "pixiv.cat");
                    return illustInfo;
                }
                catch (Exception e)
                {
                    if (!Directory.Exists(CQSave.AppDirectory + "error\\" + "IllustInfo\\"))
                        Directory.CreateDirectory(CQSave.AppDirectory + "error\\" + "IllustInfo\\");
                    IniConfig ini = new IniConfig(CQSave.AppDirectory + "error\\" + "IllustInfo\\" + $"{DateTime.Now:yyyyMMddHHss}.log");
                    ini.Object["Error"]["Message"] = e.Message;
                    ini.Object["Error"]["StackTrace"] = e.StackTrace;
                    ini.Object["Error"]["Object"] = returnstr;
                    ini.Save();
                    MainSave.CQLog.Info("图片详情", $"解析失败，错误信息:{e.Message}");
                    IllustInfo illustInfo = new IllustInfo()
                    {
                        IllustText = "图片解析失败，作品不存在或被删除",
                        IllustCQCode = CQApi.CQCode_Image("Error.jpg")
                    };
                    return illustInfo;
                }
            }
        }

        public static IllustInfo GetHotSearch(string keyword)
        {
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                string url = $"https://api.pixivic.com/illustrations?illustType=illust&searchType=original&maxSanityLevel=6&page={new Random().Next(1, 6)}&keyword={HttpTool.UrlEncode(keyword)}&pageSize=10";
                string returnstr = string.Empty;
                try
                {
                    string authCode = PublicVariables.PixivicAuth;
                    if (string.IsNullOrEmpty(authCode))
                    {
                        MainSave.CQLog.Info("未填写授权码", "搜图需要在数据目录的Config.ini文件内，Config字段的PixivicAuth值内填入获取到的授权码");
                        throw new Exception();
                    }
                    http.Encoding = Encoding.UTF8;
                    http.Headers.Add("Authorization", authCode);

                    returnstr = http.DownloadString(url);
                    Pixiv_HotSearch hotSearch = JsonConvert.DeserializeObject<Pixiv_HotSearch>(returnstr);
                    IllustInfo illustInfo = new IllustInfo();
                    Datum info;
                    if (hotSearch.data.Count != 0)
                    {
                        if (CQSave.R18 is false)
                        {
                            var result = hotSearch.data.Where(x => !x.tags.Any(y => y.name.Contains("R-18")))
                                .OrderBy(x => Guid.NewGuid().ToString());
                            info = result.FirstOrDefault();
                            if (info != null)
                            {
                                if (result.Count() != hotSearch.data.Count)
                                {
                                    if (hotSearch.data.Count != 0)
                                        MainSave.CQLog.Info("R18拦截", $"拦截了 {hotSearch.data.Count - result.Count()} 个搜索结果");
                                }
                                illustInfo = new IllustInfo()
                                {
                                    IllustText = Pixiv_HotSearch.GetSearchText(info),
                                    IllustCQCode = Pixiv_HotSearch.GetSearchPic(info),
                                    IllustUrl = info.imageUrls[0].original.Replace("pximg.net", "pixiv.cat")
                                };
                            }
                            else
                            {
                                if (hotSearch.data.Count != 0)
                                    MainSave.CQLog.Info("R18拦截", $"拦截了 {hotSearch.data.Count} 个搜索结果");
                                illustInfo = new IllustInfo()
                                {
                                    IllustText = "设置内限制级图片，不予显示",
                                    IllustCQCode = new CQCode(CQFunction.Image, new KeyValuePair<string, string>("file", "Error.jpg"))
                                };
                                return illustInfo;
                            }
                        }
                        else
                        {
                            info = hotSearch.data.OrderBy(x => Guid.NewGuid().ToString()).First();
                            illustInfo = new IllustInfo()
                            {
                                IllustText = Pixiv_HotSearch.GetSearchText(info),
                                IllustCQCode = Pixiv_HotSearch.GetSearchPic(info),
                                IllustUrl = info.imageUrls[0].original.Replace("pximg.net", "pixiv.cat"),
                                R18_Flag = info.tags.Any(x => x.name.Contains("R-18"))
                            };
                        }
                    }
                    else
                    {
                        illustInfo = new IllustInfo()
                        {
                            IllustText = "搜索结果为空",
                            IllustCQCode = CQApi.CQCode_Image("Error.jpg")
                        };
                    }
                    return illustInfo;
                }
                catch (Exception e)
                {
                    if (!Directory.Exists(CQSave.AppDirectory + "error\\" + "hotsearch\\"))
                        Directory.CreateDirectory(CQSave.AppDirectory + "error\\" + "hotsearch\\");
                    IniConfig ini = new IniConfig(CQSave.AppDirectory + "error\\" + "hotsearch\\" + $"{DateTime.Now:yyyyMMddHHss}.log");
                    ini.Object["Error"]["Message"] = e.Message;
                    ini.Object["Error"]["StackTrace"] = e.StackTrace;
                    ini.Object["Error"]["Object"] = returnstr;
                    ini.Save();
                    MainSave.CQLog.Info("搜索详情", $"解析失败，错误信息:{e.Message}");
                    IllustInfo illustInfo = new IllustInfo()
                    {
                        IllustText = "解析失败，无法获取热门搜索",
                        IllustCQCode = CQApi.CQCode_Image("Error.jpg")
                    };
                    return illustInfo;
                }
            }
        }
    }
}
