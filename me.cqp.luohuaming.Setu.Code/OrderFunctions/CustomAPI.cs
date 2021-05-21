using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using me.cqp.luohuaming.Setu.Code.Helper;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class CustomAPI : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 自定义接口指令数组
        /// </summary>
        List<CustomObject> CustomAPIOrderList = new List<CustomObject>();
        public bool Judge(string destStr)
        {            
            if (File.Exists(MainSave.AppDirectory + "CustomAPI.json"))
                CustomAPIOrderList = JsonConvert.DeserializeObject<List<CustomObject>>(File.ReadAllText(MainSave.AppDirectory + "CustomAPI.json"));
            return CommonHelper.CheckCustomObject(CustomAPIOrderList, destStr);
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
            
            var functionResult = CustomAPI_Image(e.Message.Text);
            functionResult.SendID = e.FromGroup;
            result.SendObject.Add(functionResult);

            var states = e.FromGroup.SendGroupMessage(functionResult.MsgToSend[0]);
            if (functionResult.HandlingFlag)//自动撤回
            {
                IniConfig ini = MainSave.ConfigMain;
                Task task = new Task(() =>
                {
                    Thread.Sleep(ini.Object["R18"]["RevokeTime"] * 1000);
                    e.CQApi.RemoveMessage(states.Id);
                }); task.Start();
            }
            return result;
        }

        private SendText CustomAPI_Image(string orderText)
        {
            SendText sendText = new SendText();
            try
            {
                //尝试拉取图片，若有多个相同的接口则随机来一个
                CustomObject item = CustomAPIOrderList.Where(x => x.Order == orderText)
                                                                            .OrderBy(x => Guid.NewGuid().ToString()).FirstOrDefault();
                sendText.HandlingFlag = item.AutoRevoke;
                //以后要用的路径,先生成一个
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "CustomAPIPic", item.Order);
                if (!Directory.Exists(targetdir))
                {
                    Directory.CreateDirectory(targetdir);
                }
                string imagename = DateTime.Now.ToString("yyyyMMddHHss") + ".jpg";
                string fullpath = Path.Combine(targetdir, imagename);
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                })
                {
                    http.DownloadFile(item.URL, fullpath);
                }
                MainSave.CQLog.Info("自定义接口", $"图片下载成功，尝试发送");

                //GetSetu.AntiHX(fullpath);

                string imagepath = Path.Combine("CustomAPIPic", item.Order, imagename);
                sendText.MsgToSend.Add(CQApi.CQCode_Image(imagepath).ToSendString());
                return sendText;
            }
            catch (Exception exc)
            {
                sendText.MsgToSend.Add("自定义接口调用失败");
                MainSave.CQLog.Info("自定义接口", $"调用失败，错误信息：{exc.Message}");
                return sendText;
            }
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
