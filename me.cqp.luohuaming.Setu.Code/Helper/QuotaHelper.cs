using System.Collections.Generic;
using Native.Sdk.Cqp;
using Native.Tool.IniConfig.Linq;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.Code.Helper
{
    public static class QuotaHelper
    {
        /// <summary>
        /// 为用户的可用次数加1
        /// </summary>
        /// <param name="GroupID">操作的群号</param>
        /// <param name="QQID">操作的QQ号</param>
        public static void PlusMemberQuota(long GroupID, long QQID)
        {
            int countofPerson = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"][string.Format("Count{0}", QQID)].GetValueOrDefault(0);
            int countofGroup = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"]["CountofGroup"].GetValueOrDefault(0);
            MainSave.ConfigLimit
                .Object[$"Count{GroupID}"][string.Format("Count{0}", QQID)] = new IValue(--countofPerson);
            MainSave.ConfigLimit
                .Object[$"Count{GroupID}"]["CountofGroup"] = new IValue(--countofGroup);
            MainSave.ConfigLimit.Save();
        }

        /// <summary>
        /// 为用户的可用次数减1
        /// </summary>
        /// <param name="GroupID">操作的群号</param>
        /// <param name="QQID">操作的QQ号</param>
        public static void MinusMemberQuota(long GroupID, long QQID)
        {
            int countofPerson = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"][string.Format("Count{0}", QQID)].GetValueOrDefault(0);
            int countofGroup = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"]["CountofGroup"].GetValueOrDefault(0);
            MainSave.ConfigLimit
                .Object[$"Count{GroupID}"][string.Format("Count{0}", QQID)] = new IValue(++countofPerson);
            MainSave.ConfigLimit
                .Object[$"Count{GroupID}"]["CountofGroup"] = new IValue(++countofGroup);
            MainSave.ConfigLimit.Save();
        }

        /// <summary>
        /// 判断是否满足拉取图片的权限, 并发送相关文本 (处理逻辑合理性待评)
        /// </summary>
        /// <param name="GroupID">需要判断的群号</param>
        /// <param name="QQID">需要判断的QQ号</param>
        public static bool QuotaCheck(long GroupID, long QQID)
        {
            //判断能否拉取图片，须符合：在群、个人调用未达上限、群调用未达上限
            List<string> response = JudgeLegality(GroupID,QQID);
            if (response.Count != 2) return false;
            //将自定义at替换为CQ码
            response[1] = response[1].Replace("<@>", CQApi.CQCode_At(QQID).ToString());
            //发送处理后回答，内容可以是：调用成功、个人上限、群上限
            if (!string.IsNullOrEmpty(response[1]))
                MainSave.CQApi.SendGroupMessage(GroupID, response[1]);
            //若第一个数是非0说明不满足发送图片的要求
            if (response[0] != "0") return false;
            return true;
        }
        /// <summary>
        /// 判断是否符合取图的条件,若满足,减少1额度
        /// </summary>
        /// <param name="GroupID">需要判断的群号</param>
        /// <param name="QQID">需要判断的QQ号</param>
        /// <returns>string 数组，长度为2，第一个值为判断的结果，第二个值为需要发送的文本</returns>
        private static List<string> JudgeLegality(long GroupID, long QQID)
        {
            List<string> ls = new List<string>();

            int countofPerson, countofGroup, maxofPerson, maxofGroup;
            countofPerson = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"][string.Format("Count{0}", QQID)].GetValueOrDefault(0);
            countofGroup = MainSave.ConfigLimit
                .Object[$"Count{GroupID}"]["CountofGroup"].GetValueOrDefault(0);
            maxofGroup = MainSave.ConfigMain
                .Object["Config"]["MaxofGroup"].GetValueOrDefault(30);
            if (countofGroup > maxofGroup)
            {
                if (maxofGroup != 0)
                {
                    ls.Add("-1");
                    ls.Add(PublicVariables.MaxGroup);
                    return ls;
                }
            }
            maxofPerson = MainSave.ConfigMain
                .Object["Config"]["MaxofPerson"].GetValueOrDefault(5);
            if (countofPerson < maxofPerson)
            {
                MinusMemberQuota(GroupID, QQID);
                countofPerson++;
            }
            else
            {
                if (maxofPerson != 0)
                {
                    ls.Add("-1");
                    ls.Add(PublicVariables.MaxMember);
                    return ls;
                }
                else
                {
                    MinusMemberQuota(GroupID,QQID);
                    countofPerson++;
                }
            }
            MainSave.ConfigLimit.Object["Config"]["Timestamp"] = new IValue(CommonHelper.GetTimeStamp());
            MainSave.ConfigLimit.Save();
            ls.Add("0");
            ls.Add(PublicVariables.StartPullPic.Replace("<count>", (maxofPerson - countofPerson).ToString()));
            return ls;
        }
    }
}
