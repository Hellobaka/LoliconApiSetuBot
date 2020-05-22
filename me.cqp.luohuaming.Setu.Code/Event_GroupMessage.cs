using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
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
        public static bool revoke = false;
        private static Timer timer = new Timer();
        public static int messageId = 0;
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            //读取自定义指令与回答
            PicHelper.ReadOrderandAnswer();
            if (e.Message.Text.Replace("＃", "#") .StartsWith(PicHelper.LoliConPic,StringComparison.CurrentCulture))
            {
                e.Handler = true;
                //判断能否拉取图片，须符合：在群、个人调用未达上限、群调用未达上限
                List<string> response = PicHelper.JudgeLegality(e);
                if (response.Count != 2) return;
                //将自定义at替换为CQ码
                response[1] = response[1].Replace("<@>", CQApi.CQCode_At(e.FromQQ).ToString());
                //-401指调用群不在配置中
                if (response[0] == "-401") return;
                //发送处理后回答，内容可以是：调用成功、个人上限、群上限
                e.CQApi.SendGroupMessage(e.FromGroup, response[1]);
                //若第一个数是非0说明不满足发送图片的要求
                if (response[0]!="0") return;
                //拉取图片，处理时间受地区与网速限制
                GetSetu setu = new GetSetu();
                List<string> pic = setu.GetSetuPic(e.Message.Text.Substring(PicHelper.LoliConPic.Length));
                try
                {
                    List<string> save = pic;
                    // 处理返回文本，替换可配置文本为结果，发送处理结果
                    string str = PicHelper.ProcessReturns(pic[0], e);
                    e.CQApi.SendGroupMessage(e.FromGroup, str);
                    if (File.Exists(CQSave.ImageDirectory + $"\\{pic[1]}"))//文件是否下载成功
                    {
                        QQMessage staues = e.CQApi.SendGroupMessage(e.FromGroup, CQApi.CQCode_Image(pic[1]));
                        if (!staues.IsSuccess)//图片发送失败
                        {
                            Setu deserialize = JsonConvert.DeserializeObject<Setu>(pic[0]);
                            List<Data> msg = deserialize.data;
                            e.CQApi.SendGroupMessage(e.FromGroup, PicHelper.SendPicFailed.Replace("<url>", msg[0].url));
                            return;
                        }
                        if (revoke)
                        {
                            messageId = staues.Id;
                            IniConfig ini = new IniConfig(CQSave.AppDirectory + "Config.ini");ini.Load();
                            timer.Interval = ini.Object["R18"]["RevokeTime"] * 1000;
                            timer.Elapsed += RevokePic;
                            timer.Enabled = true;
                        }
                    }
                    else
                    {
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

        private void RevokePic(object sender, ElapsedEventArgs e)
        {
            CQSave.cq.RemoveMessage(messageId);
            timer.Enabled = false;
        }
    }
}
