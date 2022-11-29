using System.Collections.Generic;
using System.Text;
using Native.Sdk.Cqp.EventArgs;

namespace me.cqp.luohuaming.Setu.PublicInfos
{
    public interface IOrderModel
    {
        string GetOrderStr();
        bool Judge(string destStr);
        FunctionResult Progress(CQGroupMessageEventArgs e);
        FunctionResult Progress(CQPrivateMessageEventArgs e);
    }
    public class CustomObject
    {
        /// <summary>
        /// 是否开启接口
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// 指令
        /// </summary>
        public string Order;
        /// <summary>
        /// API链接
        /// </summary>
        public string URL;
        /// <summary>
        /// API链接
        /// </summary>
        public string Path;
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark;
        /// <summary>
        /// 调用次数限制
        /// </summary>
        public bool Usestrict;
        /// <summary>
        /// 自动撤回
        /// </summary>
        public bool AutoRevoke;
    }
    public class JsonToDeserize
    {
        public bool Enabled { get; set; }
        public string Order { get; set; }
        public string url { get; set; }
        public string picPath { get; set; }
        public string Text { get; set; }
        public bool AutoRevoke { get; set; }
    }
    public class SauceNao_Result
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
                    sb.AppendLine("相似度:" + item.header.similarity + "%");
                    if (!string.IsNullOrEmpty(item.data.title))
                        sb.AppendLine("标题:" + item.data.title);
                    if (!string.IsNullOrEmpty(item.data.member_name))
                        sb.AppendLine("作者:" + item.data.member_name);
                    if (item.data.pixiv_id != null)
                        sb.AppendLine("Pixiv_ID:" + item.data.pixiv_id);
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
    public class DelayAPI_Save
    {
        public DelayAPI_Save(long groupID, long qQID)
        {
            GroupID = groupID;
            QQID = qQID;
        }

        public long GroupID { get; set; }
        public long QQID { get; set; }
    }

    public class TraceMoe_Result
    {
        public class Data
        {
            public int frameCount { get; set; }
            public string error { get; set; }
            public Result[] result { get; set; }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (result.Length > 0)
                {
                    sb.AppendLine("相似度第一的是：");
                    if (result[0].anilist != null)
                    {
                        sb.AppendLine($"名称：{result[0].anilist.title.native}");
                        sb.AppendLine($"罗马音名称：{result[0].anilist.title.romaji}");
                    }
                    sb.AppendLine($"相似度：{result[0].similarity * 100:f2}%");
                    sb.AppendLine($"集数：{result[0].episode}");
                    sb.AppendLine($"大约出现在：{(int)result[0].from / 60}:{(int)result[0].from % 60}" +
                        $" - {(int)result[0].to / 60}:{(int)result[0].to % 60}");
                    sb.Append($"每日搜索额度有限，请勿倒垃圾");
                }
                else
                {
                    sb.AppendLine($"未找到相似的动画");
                }
                return sb.ToString();
            }
        }

        public class Result
        {
            public Anilist anilist { get; set; }
            public string filename { get; set; }
            public int episode { get; set; }
            public float from { get; set; }
            public float to { get; set; }
            public float similarity { get; set; }
            public string video { get; set; }
            public string image { get; set; }
        }

        public class Anilist
        {
            public int id { get; set; }
            public int idMal { get; set; }
            public Title title { get; set; }
            public string[] synonyms { get; set; }
            public bool isAdult { get; set; }
        }

        public class Title
        {
            public string native { get; set; }
            public string romaji { get; set; }
            public string english { get; set; }
        }
    }
}
