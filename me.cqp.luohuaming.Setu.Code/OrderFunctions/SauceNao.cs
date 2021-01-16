using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class SauceNao : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(PublicVariables.SauceNaoSearch))
            {
                PublicVariables.SauceNaoSearch = Guid.NewGuid().ToString();
            }
            return PublicVariables.SauceNaoSearch;
        }

        public bool Judge(string destStr)
        {
            return destStr.Replace("＃", "#").StartsWith(GetOrderStr());
        }

        public FunctionResult Progress(CQGroupMessageEventArgs e)
        {
            FunctionResult result = new FunctionResult()
            {
                Result = true,
                SendFlag = false,
            };
            //检查额度限制
            if (QuotaHelper.QuotaCheck(e.FromGroup, e.FromQQ) is false)
            {
                return result;
            }
            SendText sendText = new SendText();
            sendText.SendID = e.FromGroup;
            result.SendObject.Add(sendText);

            if (e.Message.CQCodes.Count != 0)
            {
                foreach (var item in e.Message.CQCodes)
                {
                    if (item.IsImageCQCode)
                    {
                        SauceNao_Call(item, e);
                        Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                result.SendFlag = true;
                MainSave.SauceNao_Saves.Add(new DelayAPI_Save(e.FromGroup.Id, e.FromQQ.Id));
                sendText.MsgToSend.Add("请在接下来的一条消息内发送需要搜索的图片");
            }
            return result;
        }
        /// <summary>
        /// SauceNao调用
        /// </summary>
        /// <param name="cqcode">图片CQ码</param>
        /// <param name="e"></param>
        public static void SauceNao_Call(CQCode cqcode, CQGroupMessageEventArgs e)
        {
            string url = "https://saucenao.com/search.php?output_type=2&api_key=56faa0cddf50860330a295e0c331be7c4b4c021f&db=999&numres=3&url=";
            url += CommonHelper.GetImageURL(cqcode.ToSendString());
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
                    Directory.CreateDirectory(MainSave.ImageDirectory + "SauceNaotemp");
                    var result = JsonConvert.DeserializeObject<SauceNao_Result.SauceNAO>(http.DownloadString(url));
                    e.CQLog.Info("SauceNao识图", "结果获取成功，正在拉取缩略图");                    
                    int count = 1;
                    result.results = result.results.Take(1).ToList();
                    string str = result.ToString();
                    foreach (var item in result.results)
                    {
                        try
                        {
                            string filename = Guid.NewGuid().ToString().ToString();
                            http.DownloadFile(item.header.thumbnail, $@"{MainSave.ImageDirectory}\SauceNaotemp\{filename}.jpg");
                            str = str.Replace($"{{{count}}}", CQApi.CQCode_Image($@"\SauceNaotemp\{filename}.jpg").ToSendString());
                        }
                        catch
                        {
                            str = str.Replace($"{{{count}}}", item.header.thumbnail);
                        }
                        finally { count++; }
                    }
                    e.FromGroup.SendGroupMessage(str);
                    List<int> ls = result.results.Where(x => x.data.pixiv_id.HasValue).Select(x => x.data.pixiv_id.Value).ToList();
                    if (ls.Count != 0)
                    {
                        e.FromGroup.SendGroupMessage("有Pixiv图片信息，尝试拉取原图...");
                        foreach (var item in ls)
                        {
                            try
                            {
                                http.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                                if (!File.Exists($@"{MainSave.ImageDirectory}\LoliconPic\${item}.jpg"))
                                {
                                    dynamic jObject = JObject.Parse(http.UploadString("https://api.pixiv.cat/v1/generate", $"p={item}"));
                                    string pixiv_url = string.Empty;
                                    try
                                    {
                                        var urllist = jObject.original_urls_proxy;
                                        pixiv_url = urllist[0];
                                        e.FromGroup.SendGroupMessage("此图为多P图片，选择第一P下载");
                                        MainSave.CQLog.Info("SauceNao识图", "此图为多P图片，选择第一P下载");
                                    }
                                    catch
                                    {
                                        pixiv_url = jObject.Value<string>("original_url_proxy");
                                    }
                                    if (!Directory.Exists($@"{MainSave.ImageDirectory}\LoliconPic"))
                                        Directory.CreateDirectory($@"{MainSave.ImageDirectory}\LoliconPic");
                                    http.DownloadFile(pixiv_url, $@"{MainSave.ImageDirectory}\LoliconPic\{item}.jpg");
                                    MainSave.CQLog.Info("SauceNao识图", $"pid={item}的图片下载成功，尝试发送");
                                }
                                QQMessage staues = e.FromGroup.SendGroupMessage(CQApi.CQCode_Image($@"\LoliconPic\{item}.jpg"));                                
                            }
                            catch (Exception exc)
                            {
                                e.FromGroup.SendGroupMessage($"pid={item}的图片拉取失败,错误信息:{exc.Message}");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    e.CQLog.Info("SauceNao搜图", $"搜索失败，错误信息:{exc.Message}在{exc.StackTrace}");
                    e.FromGroup.SendGroupMessage($"拉取失败，错误信息:{exc.Message}");
                }
                try
                {
                    string path = $@"{MainSave.ImageDirectory}\SauceNaotemp";
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
                catch { }
            }
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
