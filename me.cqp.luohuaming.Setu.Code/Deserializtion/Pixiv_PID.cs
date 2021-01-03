namespace me.cqp.luohuaming.Setu.Code.Deserializtion
{
    public class Pixiv_PID
    {
        public string message { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public int id { get; set; }
        public int artistId { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string caption { get; set; }
        public Artistpreview artistPreView { get; set; }
        public Tag[] tags { get; set; }
        public Imageurl[] imageUrls { get; set; }
        public string[] tools { get; set; }
        public string createDate { get; set; }
        public int pageCount { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int sanityLevel { get; set; }
        public int restrict { get; set; }
        public int totalView { get; set; }
        public int totalBookmarks { get; set; }
        public int xrestrict { get; set; }
    }

    public class Artistpreview
    {
        public int id { get; set; }
        public string name { get; set; }
        public string account { get; set; }
        public string avatar { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string translatedName { get; set; }
        public int id { get; set; }
    }

    public class Imageurl
    {
        public string squareMedium { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
        public string original { get; set; }
    }
}
