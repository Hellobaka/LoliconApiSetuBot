using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using me.cqp.luohuaming.Setu.PublicInfos;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using me.cqp.luohuaming.Setu.PublicInfos.Config;

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
            { "SauceNao","#nao" },
            { "TraceMoeSearch","#trace" },
            { "YandereIDSearch","#yid" },
            { "YandereTagSearch","#ytag" },
            {"StartPullPic", "拉取图片中~至少需要15s……\n你今日剩余调用次数为<count>次(￣▽￣)"},
            {"Sucess", "机器人当日剩余调用次数:<quota>\n下次额度恢复时间为:<quota_time>\ntitle: <title>\nauthor: <author>\np: <p>\npid: <pid>"},
            {"MaxMember","你当日所能调用的次数已达上限(￣▽￣)" },
            {"MaxGroup","本群当日所能调用的次数已达上限(￣▽￣)" }
        };
        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            text_LoliConPic.Text = OrderConfig.LoliconPicOrder;
            text_ClearLimit.Text = OrderConfig.ClearLimitOrder;
            text_PIDSearch.Text = OrderConfig.PIDSearchOrder;
            text_SauceNao.Text = OrderConfig.SauceNaoSearchOrder;
            text_TraceMoeSearch.Text = OrderConfig.TraceMoeSearchOrder;
            text_YandereIDSearch.Text = OrderConfig.YandereIDSearchOrder;
            text_YandereTagSearch.Text = OrderConfig.YandereTagSearchOrder;

            text_StartPullPic.Text = AppConfig.StartResponse;
            text_Sucess.Text = AppConfig.SuccessResponse;
            text_MaxMember.Text = AppConfig.MaxMemberResoponse;
            text_MaxGroup.Text = AppConfig.MaxGroupResoponse;
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            ConfigHelper.SetConfig("LoliconPicOrder", text_LoliConPic.Text);
            ConfigHelper.SetConfig("ClearLimitOrder", text_ClearLimit.Text);
            ConfigHelper.SetConfig("PIDSearchOrder", text_PIDSearch.Text);
            ConfigHelper.SetConfig("SauceNaoSearchOrder", text_SauceNao.Text);
            ConfigHelper.SetConfig("TraceMoeSearchOrder", text_TraceMoeSearch.Text);
            ConfigHelper.SetConfig("YandereIDSearchOrder", text_YandereIDSearch.Text);
            ConfigHelper.SetConfig("YandereTagSearchOrder", text_YandereTagSearch.Text);

            ConfigHelper.SetConfig("StartResponse", text_StartPullPic.Text);
            ConfigHelper.SetConfig("Sucess", text_Sucess.Text);
            ConfigHelper.SetConfig("MaxMemberResoponse", text_MaxMember.Text);
            ConfigHelper.SetConfig("MaxGroupResoponse", text_MaxGroup.Text);

            ConfigHelper.InitConfig();
        }

        private void button_Reset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var uiitem in stackpanel_1.Children)
            {
                var textboxTemp = uiitem as TextBox;
                try
                {
                    if (uiitem.GetType().Name == "TextBox")
                        textboxTemp.Text = defaultValue[textboxTemp.Name.Replace("text_", "")];
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
