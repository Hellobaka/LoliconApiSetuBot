using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code.Deserializtion.PixivRank
{
    #region --排行榜解析类--
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
        public int id { get; set; }
        public string name { get; set; }
        public string account { get; set; }
        public ProfileImageUrls profile_image_urls { get; set; }
        public bool is_followed { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string translated_name { get; set; }
    }

    public class Series
    {
        public int id { get; set; }
        public string title { get; set; }
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

    public class Illust
    {
        public int id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public ImageUrls image_urls { get; set; }
        public string caption { get; set; }
        public int restrict { get; set; }
        public User user { get; set; }
        public IList<Tag> tags { get; set; }
        public IList<string> tools { get; set; }
        public string create_date { get; set; }
        public int page_count { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int sanity_level { get; set; }
        public int x_restrict { get; set; }
        public Series series { get; set; }
        public object meta_single_page { get; set; }
        public IList<MetaPage> meta_pages { get; set; }
        public int total_view { get; set; }
        public int total_bookmarks { get; set; }
        public bool is_bookmarked { get; set; }
        public bool visible { get; set; }
        public bool is_muted { get; set; }
    }
    #endregion

    class Pixiv_Rank
    {		
        public IList<Illust> illusts { get; set; }
        public string next_url { get; set; }

        public string GetSingleRankText(Illust info)
        {

            return string.Empty;
        }

        public Image GetRankImage(IList<Illust> info)
        {

            foreach(var item in info)
            {
                
            }
            return null;
        }
        public Image GetSingleRankInfoPic(Illust info)
        {

            return null;
        }
    }
}
