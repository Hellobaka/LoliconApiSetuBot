using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Deserializtion.HotSearch;
using me.cqp.luohuaming.Setu.Code.Deserializtion.PixivIllust;
using me.cqp.luohuaming.Setu.Code.Deserializtion.PixivR18Illust;
using me.cqp.luohuaming.Setu.Code.Deserializtion.PixivRank;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;

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
            string url = $"https://api.imjad.cn/pixiv/v2/?type=illust&id={id}";
            string returnstr = string.Empty;
            try
            {            
                returnstr = Encoding.UTF8.GetString(HttpWebClient.Get(url));
                try
                {
                    Pixiv_Illust infobase = JsonConvert.DeserializeObject<Pixiv_Illust>(returnstr);
                    Deserializtion.PixivIllust.Illust info = infobase.illust;
                    IllustInfo illustInfo = new IllustInfo()
                    {
                        IllustText = Pixiv_Illust.GetIllustReturnText(info),
                        IllustCQCode = Pixiv_Illust.GetIllustPic(info),
                    };
                    illustInfo.IllustUrl = info.meta_single_page.original_image_url.Replace("pximg.net", "pixiv.cat");
                    return illustInfo;
                }
                catch
                {
                    Pixiv_R18Illust infobase = JsonConvert.DeserializeObject<Pixiv_R18Illust>(returnstr);
                    Deserializtion.PixivR18Illust.Illust info = infobase.illust;
                    IllustInfo illustInfo = new IllustInfo()
                    {
                        IllustText = Pixiv_R18Illust.GetIllustReturnText(info),
                        IllustCQCode = Pixiv_R18Illust.GetIllustPic(info),
                    };
                    if (info.meta_single_page.Count != 0)
                        illustInfo.IllustUrl = info.meta_single_page[0].original_image_url.Replace("pximg.net", "pixiv.cat");
                    return illustInfo;
                }
            }
            catch(Exception e)
            {
                if (!Directory.Exists(CQSave.AppDirectory + "error\\" + "IllustInfo\\"))
                    Directory.CreateDirectory(CQSave.AppDirectory + "error\\" + "IllustInfo\\");
                IniConfig ini = new IniConfig(CQSave.AppDirectory +"error\\"+"IllustInfo\\"+ $"{DateTime.Now:yyyyMMddHHss}.log");
                ini.Object["Error"]["Message"] = e.Message;
                ini.Object["Error"]["StackTrace"] = e.StackTrace;
                ini.Object["Error"]["Object"] = returnstr;
                ini.Save();
                CQSave.cqlog.Info("图片详情", $"解析失败，错误信息:{e.Message}");
                IllustInfo illustInfo = new IllustInfo()
                {
                    IllustText = "图片解析失败，作品不存在或被删除",
                    IllustCQCode = CQApi.CQCode_Image("Error.jpg")
                };
                return illustInfo;
            }
        }

        public static IllustInfo GetHotSearch(string keyword)
        {
            string url = $"https://api.pixivic.com/illustrations?illustType=illust&searchType=original&maxSanityLevel=6&page={new Random().Next(1,6)}&keyword={HttpTool.UrlEncode(keyword)}&pageSize=10";
            string returnstr = string.Empty;
            try
            {
                returnstr = Encoding.UTF8.GetString(HttpWebClient.Get(url));
                Pixiv_HotSearch hotSearch = JsonConvert.DeserializeObject<Pixiv_HotSearch>(returnstr);
                IllustInfo illustInfo=new IllustInfo();
                if (hotSearch.data.Count != 0)
                {
                    Datum info = hotSearch.data[new Random().Next(hotSearch.data.Count)];
                    illustInfo = new IllustInfo()
                    {
                        IllustText = Pixiv_HotSearch.GetSearchText(info),
                        IllustCQCode = Pixiv_HotSearch.GetSearchPic(info),
                        IllustUrl = info.imageUrls[0].original.Replace("pximg.net", "pixiv.cat")
                    };
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
            catch(Exception e)
            {
                if (!Directory.Exists(CQSave.AppDirectory + "error\\" + "hotsearch\\"))
                    Directory.CreateDirectory(CQSave.AppDirectory + "error\\" + "hotsearch\\");
                IniConfig ini = new IniConfig(CQSave.AppDirectory + "error\\" + "hotsearch\\" + $"{DateTime.Now:yyyyMMddHHss}.log");
                ini.Object["Error"]["Message"] = e.Message;
                ini.Object["Error"]["StackTrace"] = e.StackTrace;
                ini.Object["Error"]["Object"] = returnstr;
                ini.Save();
                CQSave.cqlog.Info("搜索详情", $"解析失败，错误信息:{e.Message}");
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
