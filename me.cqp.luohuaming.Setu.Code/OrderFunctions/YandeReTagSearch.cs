using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class YandeReTagSearch : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.YandereTagSearch))
            {
                PublicVariables.YandereTagSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.YandereTagSearch;
        }

        public bool Judge(string destStr)
        {
            return destStr.StartsWith(GetOrderStr());
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
                string tagName = e.Message.Text.Substring(GetOrderStr().Length).Trim();
                var searchResult = TagSearch(tagName);
                if (searchResult.Count > 0 && searchResult[0] != 0)
                {
                    int id = searchResult.OrderBy(x => Guid.NewGuid().ToString()).First();
                    var pic = YandeRePic.GetYandePic(id);
                    Directory.CreateDirectory(Path.Combine(MainSave.ImageDirectory, "YandeRePic"));
                    e.FromGroup.SendGroupMessage(pic.ToString());
                    using (HttpWebClient http = new HttpWebClient()
                    {
                        TimeOut = 10000,
                        Encoding = Encoding.UTF8,
                        Proxy = MainSave.Proxy,
                        AllowAutoRedirect = true,
                    })
                    {
                        string fileName = Path.Combine(MainSave.ImageDirectory, "YandeRePic", $"{pic.ID}.jpg");
                        if (File.Exists(fileName) is false)
                            http.DownloadFile(pic.PicLargeURL, fileName);
                    }
                    sendText.MsgToSend.Add(CQApi.CQCode_Image(Path.Combine("YandeRePic", $"{pic.ID}.jpg")).ToSendString());
                }
                else
                {
                    sendText.MsgToSend.Add("没有找到结果呢，请查看是否使用了正确的tag名称");
                }
            }
            catch (Exception exc)
            {
                e.CQLog.Info("YandeReTag解析出错", $"错误信息: {exc.Message} 错误位置: {exc.StackTrace}");
                sendText.MsgToSend.Add("解析出错，查看日志获取详细信息");
            }
            result.SendObject.Add(sendText);
            return result;
        }
        static string Xpath_List = "/html/body/div[8]/div[1]/div[2]/div[4]";
        public static List<int> TagSearch(string tagName)
        {
            string url = "https://yande.re/post?tags=" + tagName.Replace(" ", "_");
            using (HttpWebClient http = new HttpWebClient()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            })
            {
                //http.Proxy = new System.Net.WebProxy { Address = new Uri("http://127.0.0.1:1080") };
                string rawHtml = http.DownloadString(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(rawHtml);
                var picNode = doc.DocumentNode.SelectSingleNode(Xpath_List);
                List<int> picID = new List<int>();
                if (picNode != null && picNode.ChildNodes[1].Name == "ul")
                {
                    foreach (var item in picNode.ChildNodes[1].ChildNodes)
                    {
                        if(item.Name=="li")
                            picID.Add(Convert.ToInt32(item.Attributes["id"].Value.Replace("p", "")));
                    }
                }
                else
                {
                    picID.Add(0);
                }
                return picID;
            }
        }
        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
