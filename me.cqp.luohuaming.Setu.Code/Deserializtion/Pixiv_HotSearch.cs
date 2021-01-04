using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using PublicInfos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code.Deserializtion.HotSearch
{
    #region --热门搜索解析类--
    public class ArtistPreView
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string account { get; set; }
        public string avatar { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string translatedName { get; set; }
        public int? id { get; set; }
    }

    public class ImageUrl
    {
        public string squareMedium { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
        public string original { get; set; }
    }

    public class Datum
    {
        public int? id { get; set; }
        public int? artistId { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string caption { get; set; }
        public ArtistPreView artistPreView { get; set; }
        public IList<Tag> tags { get; set; }
        public IList<ImageUrl> imageUrls { get; set; }
        public IList<object> tools { get; set; }
        public string createDate { get; set; }
        public int? pageCount { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? sanityLevel { get; set; }
        public int? restrict { get; set; }
        public int? totalView { get; set; }
        public int? totalBookmarks { get; set; }
        public int? xrestrict { get; set; }
    }
    #endregion

    public class Pixiv_HotSearch
    {
        public string message { get; set; }
        public IList<Datum> data { get; set; }

        public static string GetSearchText(Datum info)
        {
            string text = $"标题:{info.title}\n作者:{info.artistPreView.name}\npid={info.id}\n创作日期:{info.createDate}\n浏览数:{info.totalView}\n收藏数:{info.totalBookmarks}";
            MainSave.CQLog.Info("搜索详情", "详情获取成功，正在拉取图片");
            return text;
        }
        public static CQCode GetSearchPic(Datum info)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "data", "image", "LoliconPic", $"{info.id}.jpg");
            var b = Path.GetDirectoryName(path);
            if (!Directory.Exists(b))
                Directory.CreateDirectory(b);
            string pathcqcode = Path.Combine("LoliConPic", $"{info.id}.jpg");
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        string url = info.imageUrls[0].original.Replace("pximg.net", "pixiv.cat");
                        http.DownloadFile(url, path);
                        CommonHelper.AntiHX(path);
                        MainSave.CQLog.Info("搜索详情", "图片下载成功，正在尝试发送");

                    }
                }
                catch (Exception e)
                {
                    MainSave.CQLog.Info("搜索详情", $"图片下载失败，错误信息:{e.Message}\n{e.StackTrace}");
                    return CQApi.CQCode_Image("Error.jpg");
                }
            }
            return CQApi.CQCode_Image(pathcqcode);
        }

    }
}

