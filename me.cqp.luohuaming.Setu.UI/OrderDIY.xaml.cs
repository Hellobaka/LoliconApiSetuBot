using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using PublicInfos;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// OrderDIY.xaml 的交互逻辑
    /// </summary>
    public partial class OrderDIY : Page
    {
        public OrderDIY()
        {
            InitializeComponent();
        }

        #region 文本框的默认值
        /// <summary>
        /// 文本框的默认值
        /// </summary>
        private Dictionary<string, string> defaultValue = new Dictionary<string, string>
        {
            { "ClearLimit","#clear" },
            { "LoliConPic","#setu" },
            { "PIDSearch","#pid" },
            { "HotSearch","#搜图" },
            { "SauceNao","#nao" },
            { "TraceMoeSearch","#trace" },
            { "YandereIDSearch","#yid" },
            { "YandereTagSearch","#ytag" },
            {"DownloadFailed" ,"下载错误，次数已归还" },
            { "ExtraError","发生错误，请尝试重新调用，错误信息:<wrong_msg>" },
            { "OutofQuota","超出额度，次数已归还\n下次额度恢复的时间是:<quota_time>" },
            {"SendPicFailed", "由于不可抗力导致图被吞，复制进浏览器看看吧:<url>"},
            {"StartPullPic", "拉取图片中~至少需要15s……\n你今日剩余调用次数为<count>次(￣▽￣)"},
            {"Sucess", "机器人当日剩余调用次数:<quota>\n下次额度恢复时间为:<quota_time>\ntitle: <title>\nauthor: <author>\np: <p>\npid: <pid>"},
            {"MaxMember","你当日所能调用的次数已达上限(￣▽￣)" },
            {"MaxGroup","本群当日所能调用的次数已达上限(￣▽￣)" }
        };
        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string path = $@"{MainSave.AppDirectory}Config.ini";
            IniConfig ini = new IniConfig(path);
            ini.Load();
            foreach (var uiitem in stackpanel_1.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        textboxTemp.Text = ini.Object["OrderDIY"][textboxTemp.Name.Replace("text_", "")].GetValueOrDefault("")
                            .Replace(@"\n","\n");
                }
                catch { }
            }
            foreach (var uiitem in stackpanel_AnwDIY.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        textboxTemp.Text = ini.Object["AnswerDIY"][textboxTemp.Name.Replace("text_", "")].GetValueOrDefault("")
                            .Replace(@"\n", "\n");
                }
                catch { }
            }
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            IniConfig ini = MainSave.ConfigMain;
            ini.Object["OrderDIY"]["LoliConPic"] = new IValue(text_LoliConPic.Text);
            ini.Object["OrderDIY"]["ClearLimit"] = new IValue(text_ClearLimit.Text);
            ini.Object["OrderDIY"]["PIDSearch"] = new IValue(text_PIDSearch.Text);
            ini.Object["OrderDIY"]["HotSearch"] = new IValue(text_HotSearch.Text);
            ini.Object["OrderDIY"]["SauceNao"] = new IValue(text_SauceNao.Text);
            ini.Object["OrderDIY"]["TraceMoeSearch"] = new IValue(text_TraceMoeSearch.Text);
            ini.Object["OrderDIY"]["YandereIDSearch"] = new IValue(text_YandereIDSearch.Text);
            ini.Object["OrderDIY"]["YandereTagSearch"] = new IValue(text_YandereTagSearch.Text);

            foreach (var uiitem in stackpanel_AnwDIY.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        ini.Object["AnswerDIY"][textboxTemp.Name.Replace("text_", "")] = new IValue(textboxTemp.Text.Replace("\n",@"\n"));
                }
                catch { }
            }
            ini.Save();
        }

        private void button_Reset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var uiitem in stackpanel_1.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        textboxTemp.Text = defaultValue[textboxTemp.Name.Replace("text_","")];
                }
                catch { }
            }
            foreach (var uiitem in stackpanel_AnwDIY.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        textboxTemp.Text = defaultValue[textboxTemp.Name.Replace("text_", "")];
                }
                catch { }
            }
        }
    }
}
