using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using PublicInfos;
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
            ini.Load();
            ini.Object["R18"]["Enabled"] = new IValue((bool)Toggle_R18.IsChecked ? "1" : "0");
            PublicVariables.R18_Flag = (bool)Toggle_R18.IsChecked;
            ini.Object["R18"]["R18PicRevoke"] = new IValue((bool)Toggle_Revoke.IsChecked ? "1" : "0");
            ini.Object["R18"]["RevokeTime"] = new IValue(text_Revoke.Text);
            ini.Object["Config"]["FailedCompress"] = new IValue((bool)Toggle_CompressImg.IsChecked ? "1" : "0");

            ini.Save();
        }

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            ini.Load();
            ini.Object["R18"]["Enabled"] = new IValue((bool)Toggle_R18.IsChecked ? "1" : "0");
            ini.Object["R18"]["R18PicRevoke"] = new IValue((bool)Toggle_Revoke.IsChecked ? "1" : "0");
            ini.Object["R18"]["RevokeTime"] = new IValue(text_Revoke.Text);
            ini.Object["Config"]["FailedCompress"] = new IValue((bool)Toggle_CompressImg.IsChecked ? "1" : "0");

            ini.Save();
            btn_Apply.IsEnabled = false;
        }
        static IniConfig ini;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ini = new IniConfig(MainSave.AppDirectory + "Config.ini");
            ini.Load();
            Toggle_R18.IsChecked = ini.Object["R18"]["Enabled"].GetValueOrDefault("0") == "1";
            Toggle_Revoke.IsChecked = ini.Object["R18"]["R18PicRevoke"].GetValueOrDefault("0") == "1";
            text_Revoke.Text = ini.Object["R18"]["RevokeTime"].GetValueOrDefault("0");
            Toggle_CompressImg.IsChecked = ini.Object["Config"]["FailedCompress"].GetValueOrDefault("0") == "1";


            Toggle_R18.Click += EnabledApplyButton;
            Toggle_Revoke.Click += EnabledApplyButton;
            Toggle_CompressImg.Click += EnabledApplyButton;
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
