using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Tool.IniConfig;
using Newtonsoft.Json;

namespace me.cqp.luohuaming.Setu.Code
{
    #region --解析类--
    //INIhelper\.IniRead\((\S+), (\S+), (\S+), \S+?\)
    //INIhelper\.IniWrite\((\S+), (\S+), (\S+), \S+?\)
    //ini.Object[$1][$2].GetValueOrDefault($3)
    //ini.Object[$1][$2]=new IValue($3)
    #endregion
    public class Event_GroupMessage : IGroupMessage
    {
        public static bool revoke = false;
        private static Timer timer = new Timer();
        private static int messageId = 0;
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            //读取自定义指令与回答
            PicHelper.ReadOrderandAnswer();
            List<ItemToSave> CustomAPI=new List<ItemToSave>();
            if (File.Exists(CQSave.AppDirectory+"CustomAPI.json"))
            {
                string temp = File.ReadAllText(CQSave.AppDirectory + "CustomAPI.json");
                //反序列化
                CustomAPI = JsonConvert.DeserializeObject<List<ItemToSave>>(temp);
            }
            List<ItemToSave> LocalPic = new List<ItemToSave>();
            if (File.Exists(CQSave.AppDirectory + "LocalPic.json"))
            {
                string temp = File.ReadAllText(CQSave.AppDirectory + "LocalPic.json");
                //反序列化
                LocalPic = JsonConvert.DeserializeObject<List<ItemToSave>>(temp);
            }

            if (e.Message.Text.Replace("＃", "#") .StartsWith(PicHelper.LoliConPic,StringComparison.CurrentCulture))
            {
                if (!CanCallFunc(e)) return;

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
                        if (revoke)//自动撤回
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
                if (!CanCallFunc(e)) return;

                if (!PicHelper.IsAdmin(e))
                {
                    e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ), "权限不足，拒绝操作");
                    return;
                }
                File.Delete(CQSave.AppDirectory + "ConfigLimit.ini");
                e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ), "重置成功");
                return;
            }
            else if (PicHelper.CheckCustomAPI(CustomAPI,e))
            {
                if (!CanCallFunc(e)) return;
                PicHelper.CustomAPI_Call(CustomAPI, e);
            }
            else if (PicHelper.CheckLocalPic(LocalPic, e))
            {
                if (!CanCallFunc(e)) return;
                PicHelper.LocalPic_Call(LocalPic, e);
            }
            else if (e.Message.Text.ToLower().StartsWith("#pid"))
            {
                if (!CanCallFunc(e)) return;

                if (e.Message.Text.Trim().Length == "#pid".Length)
                {
                    e.FromGroup.SendGroupMessage("指令无效，请在指令后添加pid");
                    return;
                }
                if(!int.TryParse( e.Message.Text.Substring("#pid".Length).Replace(" ",""),out int pid))
                {
                    e.FromGroup.SendGroupMessage("指令无效，检查是否为纯数字");
                    return;
                }
                e.FromGroup.SendGroupMessage($"正在查询pid={pid}的插画信息，请等待……");
                IllustInfo illustInfo = PixivAPI.GetIllustInfo(pid);
                e.FromGroup.SendGroupMessage(illustInfo.IllustText);
                QQMessage message = e.FromGroup.SendGroupMessage(illustInfo.IllustCQCode);
                if (!message.IsSuccess && !string.IsNullOrEmpty(illustInfo.IllustUrl))
                {
                    e.FromGroup.SendGroupMessage($"图片发送失败，把链接复制进浏览器看看吧:{illustInfo.IllustUrl}");
                }
            }
            else if (e.Message.Text.Trim().StartsWith("#搜图"))
            {
                if (!CanCallFunc(e)) return;

                string keyword = e.Message.Text.Replace(" ", "").Substring("#搜图".Length);
                e.FromGroup.SendGroupMessage($"正在查询关键字为{keyword}的插画信息，请等待……");
                IllustInfo illustInfo = PixivAPI.GetHotSearch(keyword);
                e.FromGroup.SendGroupMessage(illustInfo.IllustText);
                QQMessage message = e.FromGroup.SendGroupMessage(illustInfo.IllustCQCode);
                if (!message.IsSuccess && !string.IsNullOrEmpty(illustInfo.IllustUrl))
                {
                    e.FromGroup.SendGroupMessage($"图片发送失败，把链接复制进浏览器看看吧:{illustInfo.IllustUrl}");
                }
            }
        }

        private void RevokePic(object sender, ElapsedEventArgs e)
        {
            CQSave.cq.RemoveMessage(messageId);
            timer.Enabled = false;
        }
        /// <summary>
        /// 判断是否满足拉取图片的权限
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool CanCallFunc(CQGroupMessageEventArgs e)
        {
            e.Handler = true;
            //判断能否拉取图片，须符合：在群、个人调用未达上限、群调用未达上限
            List<string> response = PicHelper.JudgeLegality(e);
            if (response.Count != 2) return false;
            //将自定义at替换为CQ码
            response[1] = response[1].Replace("<@>", CQApi.CQCode_At(e.FromQQ).ToString());
            //-401指调用群不在配置中
            if (response[0] == "-401") return false;
            //发送处理后回答，内容可以是：调用成功、个人上限、群上限
            e.CQApi.SendGroupMessage(e.FromGroup, response[1]);
            //若第一个数是非0说明不满足发送图片的要求
            if (response[0] != "0") return false;
            return true;
        }
    }
}
