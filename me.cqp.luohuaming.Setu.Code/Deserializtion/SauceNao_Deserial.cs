using System.Collections.Generic;
using System.Text;

namespace me.cqp.luohuaming.Setu.Code.Deserializtion
{
    public class SauceNao_Deserial
    {
        public class Header
        {
            public string similarity { get; set; }
            public string thumbnail { get; set; }
            public int? index_id { get; set; }
            public string index_name { get; set; }
        }

        public class Data
        {
            public IList<string> ext_urls { get; set; }
            public int? danbooru_id { get; set; }
            public int? gelbooru_id { get; set; }
            public int? sankaku_id { get; set; }
            public int? pixiv_id { get; set; }
            public int? da_id { get; set; }
            public int? yandere_id { get; set; }
            public int? konachan_id { get; set; }
            public int? member_id { get; set; }
            public int? bcy_id { get; set; }
            public string title { get; set; }
            public string material { get; set; }
            public string characters { get; set; }
            public string source { get; set; }
            public string member_name { get; set; }
            public string eng_name { get; set; }
            public string jp_name { get; set; }
        }

        public class Result
        {
            public Header header { get; set; }
            public Data data { get; set; }
        }

        public class SauceNAO
        {
            public IList<Result> results { get; set; }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (results == null)
                    return "解析失败，请尝试提供此图片的其他版本重试";
                foreach (var item in results)
                {
                    sb.AppendLine("相似度:"+item.header.similarity+"%");
                    if(!string.IsNullOrEmpty(item.data.title))
                        sb.AppendLine("标题:" + item.data.title);
                    if (!string.IsNullOrEmpty(item.data.member_name))
                        sb.AppendLine("作者:" + item.data.member_name);
                    if(item.data.pixiv_id!= null)
                        sb.AppendLine("Pixiv_ID:"+item.data.pixiv_id);
                    if (item.data.bcy_id != null)
                        sb.AppendLine("Bcy_ID:" + item.data.bcy_id);
                    if (item.data.danbooru_id != null)
                        sb.AppendLine("Danbooru_ID:" + item.data.danbooru_id);
                    if (item.data.gelbooru_id != null)
                        sb.AppendLine("Gelbooru_ID:" + item.data.gelbooru_id);
                    if (item.data.konachan_id != null)
                        sb.AppendLine("Konachan_ID:" + item.data.konachan_id);
                    if (item.data.sankaku_id != null)
                        sb.AppendLine("Sankaku_ID:" + item.data.sankaku_id);
                    if (item.data.yandere_id != null)
                        sb.AppendLine("yandere_ID:" + item.data.yandere_id);
                    if (item.data.da_id != null)
                        sb.AppendLine("Da_ID:" + item.data.da_id);
                    if (item.data.source != null)
                        sb.AppendLine("来源:" + item.data.source);
                    if (item.data.eng_name != null)
                        sb.AppendLine("英文名称:" + item.data.eng_name);
                    if (item.data.jp_name != null)
                        sb.AppendLine("日文名称:" + item.data.jp_name);
                    if (item.data.ext_urls != null)
                    {
                        sb.Append("图片链接：");
                        foreach (var item2 in item.data.ext_urls)
                        {
                             sb.AppendLine(item2);
                        }
                    }                    
                    sb.AppendLine("缩略图：{1}");
                }
                return sb.ToString();
            }
        }
    }
}
