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
using Native.Tool.IniConfig;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions
{
    public class LocalPic : IOrderModel
    {
        public string GetOrderStr()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 本地图片指令列表
        /// </summary>
        static List<CustomObject> LocalPicOrderList = new List<CustomObject>();
        public bool Judge(string destStr)
        {
            if (File.Exists(MainSave.AppDirectory + "LocalPic.json"))
                LocalPicOrderList = JsonConvert.DeserializeObject<List<CustomObject>>(File.ReadAllText(MainSave.AppDirectory + "LocalPic.json"));
            return CommonHelper.CheckCustomObject(LocalPicOrderList, destStr);
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

            var functionResult = LocalPic_Image(e.Message.Text,e.FromGroup,e.FromQQ);
            functionResult.SendID = e.FromGroup;
            result.SendObject.Add(functionResult);
            var staues = e.FromGroup.SendGroupMessage(functionResult.MsgToSend[0]);
            if (functionResult.HandlingFlag)//自动撤回
            {
                IniConfig ini = MainSave.ConfigMain;
                Task task = new Task(() =>
                {
                    Thread.Sleep(PublicVariables.R18_RevokeTime*1000);
                    e.CQApi.RemoveMessage(staues.Id);
                }); task.Start();
            }
            return result;
        }
        /// <summary>
        /// 本地图片拉取
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static SendText LocalPic_Image(string orderText,long GroupID,long QQID)
        {
            SendText result = new SendText();
            CustomObject item = LocalPicOrderList.Where(x => x.Order == orderText)
                                                                        .OrderBy(_ => Guid.NewGuid()).First();
            result.HandlingFlag = item.AutoRevoke;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(item.Path);
                FileInfo[] fileInfos = directoryInfo.GetFiles("*.*")
                    .Where(x => x.FullName.EndsWith("jpg") || x.FullName.EndsWith("gif")
                    || x.FullName.EndsWith("png") || x.FullName.EndsWith("bmp")
                    || x.FullName.EndsWith("webp") || x.FullName.EndsWith("tif") || x.FullName.EndsWith("tga")).ToArray();
                //随机取一个
                var picinfo = fileInfos.OrderBy(_ => Guid.NewGuid()).First();
                string picpathOrigin = picinfo.FullName;
                if (!Directory.Exists(MainSave.ImageDirectory + "\\LocalPic"))
                    Directory.CreateDirectory(MainSave.ImageDirectory + "\\LocalPic");
                string picpathFinal = MainSave.ImageDirectory + "\\LocalPic\\" + picinfo.Name;
                if (!File.Exists(picpathFinal))
                    File.Copy(picpathOrigin, picpathFinal);
                MainSave.CQLog.Info("本地图片接口", $"图片获取成功，尝试发送");
                CommonHelper.AntiHX(picpathFinal);
                string imageCQCodePath = Path.Combine("LocalPic", picinfo.Name);
                result.MsgToSend.Add(CQApi.CQCode_Image(imageCQCodePath).ToSendString());
                return result;
            }
            catch (Exception exc)
            {
                result.MsgToSend.Add("本地图片接口调用失败");
                MainSave.CQLog.Info("本地图片接口", $"调用失败，错误信息：{exc.Message}");
                if (item.Usestrict)
                    QuotaHelper.PlusMemberQuota(GroupID, QQID);
                return result;
            }
        }

        public FunctionResult Progress(CQPrivateMessageEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
