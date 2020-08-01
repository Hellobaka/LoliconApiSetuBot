using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code.Deserializtion.PixivIllust
{
    #region --PixivAPI解析类--
    public class ImageUrls
    {
        public string square_medium { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
    }

    public class ProfileImageUrls
    {
        public string medium { get; set; }
    }

    public class User
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string account { get; set; }
        public ProfileImageUrls profile_image_urls { get; set; }
        public bool is_followed { get; set; }
    }
    public class ImageUrls2
    {
        public string square_medium { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
        public string original { get; set; }
    }

    public class MetaPage
    {
        public ImageUrls2 image_urls { get; set; }
    }
    public class Tag
    {
        public string name { get; set; }
        public string translated_name { get; set; }
    }

    public class MetaSinglePage
    {
        public string original_image_url { get; set; }
    }

    public class Illust
    {
        public int? id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public ImageUrls image_urls { get; set; }
        public string caption { get; set; }
        public int? restrict { get; set; }
        public User user { get; set; }
        public IList<Tag> tags { get; set; }
        public IList<string> tools { get; set; }
        public string create_date { get; set; }
        public int? page_count { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? sanity_level { get; set; }
        public int? x_restrict { get; set; }
        public object series { get; set; }
        public MetaSinglePage meta_single_page { get; set; }
        public IList<MetaPage> meta_pages { get; set; }
        public int? total_view { get; set; }
        public int? total_bookmarks { get; set; }
        public bool is_bookmarked { get; set; }
        public bool visible { get; set; }
        public bool is_muted { get; set; }
        public int? total_comments { get; set; }
    }
    #endregion

    public class Pixiv_Illust
    {
        public Illust illust { get; set; }

        /// <summary>
        /// 处理插画详情返回文本
        /// </summary>
        /// <param name="info">Pixiv_Illust.illust类成员</param>
        /// <returns></returns>
        public static string GetIllustReturnText(Illust info)
        {
            string text = $"标题:{info.title}\n作者:{info.user.name}\npid={info.id}\n创作日期:{info.create_date}\n浏览数:{info.total_view}\n评论数:{info.total_comments}\n收藏数:{info.total_bookmarks}";
            CQSave.cqlog.Info("插画详情", "详情获取成功，正在拉取图片");
            return text;
        }

        /// <summary>
        /// 下载图片，返回CQ码
        /// </summary>
        /// <param name="info">Pixiv_Illust.illust类成员</param>
        /// <returns></returns>
        public static CQCode GetIllustPic(Illust info)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "data", "image", "LoliconPic", $"{info.id}.jpg");
            string pathcqcode = Path.Combine("LoliConPic", $"{info.id}.jpg");
            using (HttpWebClient http = new HttpWebClient())
            {
                http.TimeOut = 5000;
                try
                {
                    if (!File.Exists(path))
                    {
                        string url = string.Empty;
                        url = info.meta_single_page.original_image_url.Replace("pximg.net", "pixiv.cat");

                        http.DownloadFile(url, path);
                        GetSetu.AntiHX(path);
                        CQSave.cqlog.Info("插画详情", "图片下载成功，正在尝试发送");
                    }
                }
                catch (Exception e)
                {
                    CQSave.cqlog.Info("插画详情", $"图片下载失败，错误信息:{e.Message}");
                    return CQApi.CQCode_Image("Error.jpg");
                }
            }
            return CQApi.CQCode_Image(pathcqcode);
        }

    }


}
