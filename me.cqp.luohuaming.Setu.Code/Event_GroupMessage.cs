using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Tool.Http;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json;

namespace me.cqp.luohuaming.Setu.Code
{
    //INIhelper\.IniRead\((\S+), (\S+), (\S+), \S+?\)
    //INIhelper\.IniWrite\((\S+), (\S+), (\S+), \S+?\)
    //ini.Object[$1][$2].GetValueOrDefault($3)
    //ini.Object[$1][$2]=new IValue($3)
    public class Event_GroupMessage : IGroupMessage
    {
        private static string path, pathUser;
        private IniConfig ini, iniUser;
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            //定义
            //List数组内 第一个元素为信息 第二个元素为图片路径或者额度回复秒数
            //信息正常返回json，出现异常返回数字
            //401：接口调用次数达到上限
            //402：图片下载失败
            //403：其他错误（超时，404等）详情写入日志
            //-401：群不存在于配置中

            path = CQSave.AppDirectory + "\\Config.ini";
            pathUser = CQSave.AppDirectory + "\\ConfigLimit.ini";
            ini = new IniConfig(path); iniUser = new IniConfig(pathUser);
            ini.Load(); iniUser.Load();
            PicHelper.ReadOrderandAnswer();
            if (e.Message.Text.Replace("＃", "#") == PicHelper.LoliConPic)
            {
                e.Handler = true;

                string response = PicHelper.JudgeLegality(e);
                response = response.Replace("<@>", CQApi.CQCode_At(e.FromQQ).ToString());
                if (response == "-401") return;
                e.CQApi.SendGroupMessage(e.FromGroup, response);
                if (response == PicHelper.MaxGroup || response == PicHelper.MaxMember) return;
                GetSetu setu = new GetSetu();//处理时间受地区与网速限制
                List<string> pic = setu.GetSetuPic();
                try
                {
                    List<string> save = pic;
                    string str = PicHelper.ProcessReturns(pic[0], e);                    
                    if (int.TryParse(pic[1], out int quota_second))
                    {
                        //额度不足时，pic[1]返回的是额度回复的秒数
                        e.CQApi.SendGroupMessage(e.FromGroup, str);
                        return;
                    }
                    else
                    {
                        //调用正常
                        e.CQApi.SendGroupMessage(e.FromGroup, str);
                    }
                    if (File.Exists(CQSave.ImageDirectory + $"\\{pic[1]}"))//文件是否下载成功
                    {
                        QQMessage staues = e.CQApi.SendGroupMessage(e.FromGroup, CQApi.CQCode_Image(pic[1]));
                        if (!staues.IsSuccess)//图片发送失败
                        {
                            Setu deserialize = JsonConvert.DeserializeObject<Setu>(pic[0]);
                            List<Data> msg = deserialize.data;
                            e.CQApi.SendGroupMessage(e.FromGroup,PicHelper.SendPicFailed.Replace("<url>",msg[0].url));
                        }
                    }
                    else//图片下载失败
                    {
                        PicHelper.PlusMemberQuota(e);
                        e.FromGroup.SendGroupMessage(PicHelper.DownloadFailed);
                        return;
                    }
                }
                catch (Exception exc)
                {
                    e.CQApi.SendGroupMessage(e.FromGroup, $"发生未知错误,错误信息:在{exc.Source}上, 发生错误: {exc.Message}  有{exc.StackTrace}");
                }
            }
            else if (e.Message.Text == PicHelper.ClearLimit)
            {
                if (!PicHelper.IsAdmin(e))
                {
                    e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ), "权限不足，拒绝操作");
                    return;
                }
                File.Delete(CQSave.AppDirectory + "ConfigLimit.ini");
                e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ), "重置成功");
                return;
            }
        }
    }
}
