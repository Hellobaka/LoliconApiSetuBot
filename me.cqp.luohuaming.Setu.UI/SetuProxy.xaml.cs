using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using Native.Tool.Http;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// SetuProxy.xaml 的交互逻辑
    /// </summary>
    public partial class SetuProxy : Page
    {
        public SetuProxy()
        {
            InitializeComponent();
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                textblock_ErrorMsg.Visibility = Visibility.Visible;
                textblock_ErrorMsg.Foreground = Brushes.Black;

                ConfigHelper.SetConfig("ProxyEnabled", togglebutton_IsProxy.IsChecked.Value);
                ConfigHelper.SetConfig("ProxyURL", textbox_ProxyUri.Text);
                ConfigHelper.SetConfig("ProxyUserName", textbox_ProxyName.Text);
                ConfigHelper.SetConfig("ProxyPassword", textbox_ProxyPwd.Text);

                if(togglebutton_IsProxy.IsChecked.Value)
                {
                    MainSave.Proxy = new WebProxy
                    {
                        Address = new Uri(textbox_ProxyUri.Text),
                        Credentials = new NetworkCredential(textbox_ProxyName.Text, textbox_ProxyPwd.Text)
                    };
                }
                ConfigHelper.InitConfig();
                textblock_ErrorMsg.Text = $"保存成功，可点击退出返回";

            }
            catch (Exception ex)
            {
                textblock_ErrorMsg.Visibility = Visibility.Visible;
                textblock_ErrorMsg.Foreground = Brushes.DarkRed;
                textblock_ErrorMsg.Text = $"错误信息:{ex.Message}";
            }
        }

        private void button_Reset_Click(object sender, RoutedEventArgs e)
        {
            togglebutton_IsProxy.IsChecked = false;
            textbox_ProxyUri.Text = "http://127.0.0.1:1080";
            textbox_ProxyName.Text = "";
            textbox_ProxyPwd.Text = "";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            textblock_ErrorMsg.Visibility = Visibility.Hidden;
            togglebutton_IsProxy.IsChecked = AppConfig.ProxyEnabled;
            textbox_ProxyUri.Text = AppConfig.ProxyURL;
            textbox_ProxyName.Text = AppConfig.ProxyUserName;
            textbox_ProxyPwd.Text = AppConfig.ProxyPassword;
        }

        private void button_CheckProxy_Click(object sender, RoutedEventArgs e)
        {
            textbox_CheckProxy.Text = "测试开始\n";
            WebProxy proxy = new WebProxy();
            if ((bool)togglebutton_IsProxy.IsChecked)
            {
                try
                {
                    proxy.Address = new Uri(textbox_ProxyUri.Text);
                    proxy.Credentials = new NetworkCredential(textbox_ProxyName.Text, textbox_ProxyPwd.Text);
                }
                catch (Exception ex)
                {
                    textbox_CheckProxy.AppendText($"Proxy错误，设置的代理无效，信息:{ex.Message}\n");
                }
            }
            progressbar_Main.Visibility = Visibility.Visible;
            button_CheckProxy.IsEnabled = false;
            new Thread(() => CheckProxy(proxy)).Start();
        }

        public static byte[] Get(string url, WebProxy proxy)
        {
            HttpWebClient httpWebClient = new HttpWebClient
            {
                TimeOut = 5000,
                Proxy = proxy,
                AllowAutoRedirect = true,
                AutoCookieMerge = true
            };
            return httpWebClient.DownloadData(url);
        }

        public void CheckProxy(WebProxy proxy)
        {
            string[] url = new string[] { "https://www.baidu.com", "https://i.pximg.net", "https://api.lolicon.app", "https://pixiv.cat", "https://www.google.com", "https://www.pixiv.net" };
            foreach(var item in url)
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Get(item, proxy);
                    sw.Stop();
                    this.textbox_CheckProxy.Dispatcher.Invoke(() => { textbox_CheckProxy.AppendText($"与{item}的连接成功:耗时:{sw.ElapsedMilliseconds} ms\n"); });
                }
                catch (Exception ex)
                {
                    this.textbox_CheckProxy.Dispatcher.Invoke(() => { textbox_CheckProxy.AppendText($"与{item}的连接出错,信息:{ex.Message}\n"); });
                }
            }
            progressbar_Main.Dispatcher.Invoke(new Action(() => { progressbar_Main.Visibility = Visibility.Hidden; }));
            button_CheckProxy.Dispatcher.Invoke(new Action(() => { button_CheckProxy.IsEnabled = true; }));
        }
    }
}
