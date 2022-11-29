using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.API;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class SauceNao : IOrderModel
    {
        public string GetOrderStr()
        {
            if (string.IsNullOrWhiteSpace(OrderConfig.SauceNaoSearchOrder))
            {
                return Guid.NewGuid().ToString();
            }
            return OrderConfig.SauceNaoSearchOrder;
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
                SendFlag = true,
            };
            SendText sendText = new SendText
            {
                SendID = e.FromGroup
            };
            result.SendObject.Add(sendText);
            if (QuotaHistory.GroupQuotaDict[e.FromGroup] >= AppConfig.MaxGroupQuota)
            {
                sendText.MsgToSend.Add(AppConfig.MaxGroupResoponse);
                return result;
            }

            if (QuotaHistory.QueryQuota(e.FromGroup, e.FromQQ) <= 0)
            {
                sendText.MsgToSend.Add(AppConfig.MaxMemberResoponse);
                return result;
            }


            if (MainSave.SauceNao_Saves.Any(x => x.GroupID == e.FromGroup && x.QQID == e.FromQQ))
            {
                CQCode img = e.Message.CQCodes.FirstOrDefault(x => x.IsImageCQCode);
                if (img == null) return result;
                SauceNao_Call(img, e);
            }
            else
            {
                int quota = AppConfig.MaxPersonQuota - QuotaHistory.HandleQuota(e.FromGroup, e.FromQQ, -1);
                e.FromGroup.SendGroupMessage(AppConfig.StartResponse.Replace("<count>", quota.ToString()));

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
            Directory.CreateDirectory(Path.Combine(MainSave.ImageDirectory, "SauceNao"));
            Directory.CreateDirectory(Path.Combine(MainSave.ImageDirectory, "SauceNaoTemp"));
            using HttpWebClient http = new()
            {
                TimeOut = 10000,
                Encoding = Encoding.UTF8,
                Proxy = MainSave.Proxy,
                AllowAutoRedirect = true,
            };
            try
            {
                Directory.CreateDirectory(MainSave.ImageDirectory + "SauceNaotemp");
                var result = JsonConvert.DeserializeObject<SauceNao_Result.SauceNAO>(http.DownloadString(url));
                if (result == null || result.results == null || result.results.Count == 0)
                {
                    e.CQLog.Info("SauceNao识图", "拉取结果失败，建议重试");
                    e.FromGroup.SendGroupMessage("诶嘿，网络出了点小差~");
                    return;
                }
                e.CQLog.Info("SauceNao识图", "结果获取成功");
                result.results = result.results.Take(1).ToList();
                string str = result.ToString();
                e.CQLog.Info("SauceNao识图", "正在拉取缩略图");
                var naoResult = result.results.OrderByDescending(x => x.header.similarity).First();
                if ((double.TryParse(naoResult.header.similarity, out double value) ? value : -1) < 60)
                {
                    e.CQLog.Info("SauceNao识图", "相似度过低");
                    e.FromGroup.SendGroupMessage("相似度过低，没有找到对应图片");
                    return;
                }
                try
                {
                    string filename = Guid.NewGuid().ToString().ToString();
                    http.DownloadFile(naoResult.header.thumbnail, $@"{MainSave.ImageDirectory}\SauceNaotemp\{filename}.jpg");
                    str = str.Replace("{1}", CQApi.CQCode_Image($@"SauceNaotemp\{filename}.jpg").ToSendString());
                }
                catch
                {
                    str = str.Replace("{1}", naoResult.header.thumbnail);
                }
                e.FromGroup.SendGroupMessage(str);
                if (!naoResult.data.pixiv_id.HasValue) return;
                int pid = naoResult.data.pixiv_id.Value;
                e.FromGroup.SendGroupMessage("有Pixiv图片信息，尝试拉取原图...");
                if (!new DirectoryInfo(Path.Combine(MainSave.ImageDirectory, "SauceNao")).GetFiles().Any(x => x.Name.Contains(pid.ToString())))
                {
                    var fileInfo = new FileInfo(PixivAPI.DownloadPic(pid, Path.Combine(MainSave.ImageDirectory, "SauceNao")));
                    e.FromGroup.SendGroupMessage(CQApi.CQCode_Image(@"SauceNao\" + fileInfo.Name));
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

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
