using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class YandeRePic : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.YandereIDSearch))
            {
                PublicVariables.YandereIDSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.YandereIDSearch;
        }

        public bool Judge(string destStr)
        {
            if (destStr.StartsWith(GetOrderStr()) is false)
                return false;
            if (destStr.Split(' ').Length == 2)
                return true;
            return false;
        }

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult
            {
                Result = true,
                SendFlag = true,
            };
            SendText sendText = new SendText
            {
                SendID = e.FromGroup,
            };
            //检查额度限制
            if (QuotaHelper.QuotaCheck(e.FromGroup, e.FromQQ) is false)
            {
                return result;
            }
            try
            {
                int yandeID = Convert.ToInt32(e.Message.Text.Split(' ')[1]);
                var t = GetYandePic(yandeID);
                Directory.CreateDirectory(Path.Combine(MainSave.ImageDirectory, "YandeRePic"));
                e.FromGroup.SendGroupMessage(t.ToString());
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                })
                {
                    string fileName = Path.Combine(MainSave.ImageDirectory, "YandeRePic", $"{t.ID}.jpg");
                    if(File.Exists(fileName) is false)
                        http.DownloadFile(t.PicLargeURL, fileName);
                }
                sendText.MsgToSend.Add(CQApi.CQCode_Image(Path.Combine("YandeRePic", $"{t.ID}.jpg")).ToSendString());
            }
            catch (Exception exc)
            {
                e.CQLog.Info("YandeReID解析出错",$"错误信息: {exc.Message} 错误位置: {exc.StackTrace}");
                sendText.MsgToSend.Add("解析出错，查看日志获取详细信息");
            }
            result.SendObject.Add(sendText);
            return result;
        }
        static string XPath_Pic = "/html/body/div[8]/div[1]/div[4]/div[1]/img";
        static string XPath_Large = "/html/body/div[8]/div[1]/div[3]/div[4]/ul/li[2]";
        static string XPath_Info = "/html/body/div[8]/div[1]/div[3]/div[3]/ul[1]";
        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
        public class YandeRePicDetail
        {
            public int ID { get; set; }
            public string PicURL { get; set; }
            public string PicLargeURL { get; set; }
            public int PicWidth { get; set; }
            public int PicHeight { get; set; }
            public int PicLargeWidth { get; set; }
            public int PicLargeHeight { get; set; }
            public DateTime UploadTime { get; set; }
            public string UpLoaderName { get; set; }
            public string Source { get; set; }
            public string[] Tags { get; set; }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"ID: {ID}");
                sb.AppendLine($"图片大小: {PicLargeWidth}x{PicLargeHeight}");
                sb.AppendLine($"图片上传时间: {UploadTime}");
                sb.AppendLine($"图片上传者名称: {UpLoaderName}");
                if (string.IsNullOrWhiteSpace(Source) is false)
                {
                    sb.AppendLine($"来源: {Source}");
                }
                sb.Append("Tags: ");
                foreach(var item in Tags)
                {
                    sb.Append($"{item} ");
                }
                return sb.ToString();
            }
        }
        public static YandeRePicDetail GetYandePic(int id)
        {
            string url = $"https://yande.re/post/show/{id}";
            YandeRePicDetail detail = null;
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                //http.Proxy = new System.Net.WebProxy { Address=new Uri("http://127.0.0.1:1080") };
                string rawHtml = http.DownloadString(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(rawHtml);
                var picNode = doc.DocumentNode.SelectSingleNode(XPath_Pic);
                if (picNode == null)
                {
                    var c = doc.DocumentNode.SelectSingleNode("/html/body/div[8]/div[1]/div[3]");
                    c.Remove();
                    picNode = doc.DocumentNode.SelectSingleNode(XPath_Pic);
                }
                if (picNode != null)
                {
                    detail = new YandeRePicDetail
                    {
                        ID = id,
                        Tags = picNode.Attributes["alt"].Value.Split(' '),
                        PicWidth = Convert.ToInt32(picNode.Attributes["width"].Value),
                        PicHeight = Convert.ToInt32(picNode.Attributes["height"].Value),
                        PicLargeHeight = Convert.ToInt32(picNode.Attributes["large_height"].Value),
                        PicLargeWidth = Convert.ToInt32(picNode.Attributes["large_width"].Value),
                        PicURL = picNode.Attributes["src"].Value
                    };
                    picNode = doc.DocumentNode.SelectSingleNode(XPath_Info);
                    if (picNode.Name == "ul")
                    {
                        foreach (var item in picNode.ChildNodes)
                        {
                            int length = item.InnerText.IndexOf(":");
                            if (length < 0)
                                continue;
                            string itemName = item.InnerText.Substring(0, length);
                            switch (itemName)
                            {
                                case "Posted":                                    
                                    var t = DateTime.TryParseExact(item.SelectSingleNode("a[1]").Attributes["title"].Value
                                        , "ddd MMM dd HH:mm:ss yyyy"
                                        , new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.AdjustToUniversal,out DateTime uploadTime);
                                    if(t is false)
                                    {
                                        uploadTime = DateTime.ParseExact(item.SelectSingleNode("a[1]").Attributes["title"].Value
                                        , "ddd MMM  d HH:mm:ss yyyy"
                                        , new System.Globalization.CultureInfo("en-US"));
                                    }
                                    detail.UploadTime = uploadTime;
                                    detail.UpLoaderName = item.SelectSingleNode("a[2]").ChildNodes[0].InnerText;
                                    break;
                                case "Source":
                                    detail.Source = item.SelectSingleNode("a[1]").Attributes["href"].Value;
                                    break;
                                default:
                                    break;
                            }
                        }
                        detail.PicLargeURL = doc.DocumentNode.SelectSingleNode(XPath_Large)
                            .ChildNodes[0].Attributes["href"].Value;
                    }
                }
                else
                {
                    MainSave.CQLog.Info("Yande识别模式错误", "Xpath填写错误，联系作者更正");
                }
            }
            return detail;
        }
    }
}
