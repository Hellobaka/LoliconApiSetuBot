using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using System.Windows;
using System.Windows.Controls;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// ExtraConfig.xaml 的交互逻辑
    /// </summary>
    public partial class ExtraConfig : Page
    {
        public ExtraConfig()
        {
            InitializeComponent();
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            btn_Apply_Click(sender, e);
        }

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(text_Revoke.Text, out int value) is false)
            {
                //snackbar
                return;
            }
            ConfigHelper.SetConfig("R18", Toggle_R18.IsChecked.Value);
            ConfigHelper.SetConfig("R18_PicRevoke", Toggle_Revoke.IsChecked);
            ConfigHelper.SetConfig("R18_RevokeTime", value);

            ConfigHelper.InitConfig();
            btn_Apply.IsEnabled = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Toggle_R18.IsChecked = AppConfig.R18;
            Toggle_Revoke.IsChecked = AppConfig.R18_PicRevoke;
            text_Revoke.Text = AppConfig.R18_RevokeTime.ToString();

            Toggle_R18.Click += EnabledApplyButton;
            Toggle_Revoke.Click += EnabledApplyButton;
            text_Revoke.TextChanged += EnabledApplyButton;
        }

        void EnabledApplyButton(object sender, RoutedEventArgs e)
        {
            btn_Apply.IsEnabled = true;
        }

        void EnabledApplyButton(object sender, TextChangedEventArgs e)
        {
            btn_Apply.IsEnabled = true;
        }
    }
}
