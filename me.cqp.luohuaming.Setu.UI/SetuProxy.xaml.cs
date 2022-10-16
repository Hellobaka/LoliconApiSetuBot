using Native.Tool.Http;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.PublicInfos;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
                Uri uri = new Uri(textbox_ProxyUri.Text);
                ini.Object["Proxy"]["IsEnabled"] = new IValue(togglebutton_IsProxy.IsChecked.GetValueOrDefault() ? "1" : "0");
                ini.Object["Proxy"]["ProxyUri"] = new IValue(textbox_ProxyUri.Text);
                ini.Object["Proxy"]["ProxyName"] = new IValue(textbox_ProxyName.Text);
                ini.Object["Proxy"]["ProxyPwd"] = new IValue(textbox_ProxyPwd.Text);
                textblock_ErrorMsg.Visibility = Visibility.Visible;
                textblock_ErrorMsg.Foreground = Brushes.Black;
                ini.Save();
                if((bool)togglebutton_IsProxy.IsChecked)
                {
                    MainSave.Proxy = new WebProxy
                    {
                        Address = new Uri(textbox_ProxyUri.Text),
                        Credentials = new NetworkCredential(textbox_ProxyName.Text, textbox_ProxyPwd.Text)
                    };
                }
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
        static IniConfig ini=MainSave.ConfigMain;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            textblock_ErrorMsg.Visibility = Visibility.Hidden;
            togglebutton_IsProxy.IsChecked = ini.Object["Proxy"]["IsEnabled"].GetValueOrDefault("0") == "0" ? false : true;
            textbox_ProxyUri.Text = ini.Object["Proxy"]["ProxyUri"].GetValueOrDefault("http://127.0.0.1:1080");
            textbox_ProxyName.Text = ini.Object["Proxy"]["ProxyName"].GetValueOrDefault("");
            textbox_ProxyPwd.Text = ini.Object["Proxy"]["ProxyPwd"].GetValueOrDefault("");
        }
        WebProxy proxy;
        private void button_CheckProxy_Click(object sender, RoutedEventArgs e)
        {
            textbox_CheckProxy.Text = "测试开始\n";
            proxy = new WebProxy();
            if ((bool)togglebutton_IsProxy.IsChecked)
            {
                try
                {
                    string uri, username, pwd;
                    uri = textbox_ProxyUri.Text;
                    username = textbox_ProxyName.Text;
                    pwd = textbox_ProxyPwd.Text;

                    //proxy 
                    proxy.Address = new Uri(uri);
                    proxy.Credentials = new NetworkCredential(username, pwd);
                }
                catch (Exception ex)
                {
                    textbox_CheckProxy.AppendText($"Proxy错误,设置的代理无效，信息:{ex.Message}\n");
                }
            }
            progressbar_Main.Visibility = Visibility.Visible;
            button_CheckProxy.IsEnabled = false;
            ThreadStart threadStear = new ThreadStart(CheckProxy);
            Thread thd = new Thread(threadStear);
            // 开启线程
            thd.Start();
        }

        /// <summary>
        /// 向服务器发送 HTTP GET 请求
        /// </summary>
        /// <param name="url">完整的网页地址
        ///		<para>必须包含 "http://" 或 "https://"</para>
        /// </param>
        /// <param name="timeout">超时时间</param>
        /// <param name="proxy">代理 <see cref="HttpWebClient"/> 的 <see cref="WebProxy"/></param>
        /// <returns></returns>
        public static byte[] Get(string url, WebProxy proxy)
        {
            HttpWebClient httpWebClient = new HttpWebClient();
            httpWebClient.TimeOut = 5000;
            httpWebClient.Proxy = proxy;
            httpWebClient.AllowAutoRedirect = true;
            httpWebClient.AutoCookieMerge = true;
            byte[] result = httpWebClient.DownloadData(new Uri(url));
            return result;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        public void CheckProxy()
        {
            string[] url;
            if (!File.Exists($"{MainSave.AppDirectory}CheckProxy.json"))
            {
                string[] temp = { "https://www.baidu.com", "https://api.lolicon.app", "https://pixiv.cat", "https://www.google.com", "https://www.pixiv.net" };
                url = temp;
                File.WriteAllText($"{MainSave.AppDirectory}CheckProxy.json", JsonConvert.SerializeObject(url));//序列化
            }
            else
            {
                string temp = File.ReadAllText($"{MainSave.AppDirectory}CheckProxy.json");
                url = JsonConvert.DeserializeObject<string[]>(temp);//反序列化
            }
            for (int i = 0; i < url.Length; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                try
                {
                    byte[] by = Get(url[i], proxy);
                    string str = Encoding.UTF8.GetString(by);
                    sw.Stop();
                    this.textbox_CheckProxy.Dispatcher.Invoke(new Action(() => { textbox_CheckProxy.AppendText($"与{url[i]}的连接成功:耗时:{sw.ElapsedMilliseconds} ms\n"); }));
                }
                catch (Exception ex)
                {
                    this.textbox_CheckProxy.Dispatcher.Invoke(new Action(() => { textbox_CheckProxy.AppendText($"与{url[i]}的连接出错,信息:{ex.Message}\n"); }));
                }
            }
            this.progressbar_Main.Dispatcher.Invoke(new Action(() => { progressbar_Main.Visibility = Visibility.Hidden; }));
            this.button_CheckProxy.Dispatcher.Invoke(new Action(() => { button_CheckProxy.IsEnabled = true; }));
        }
    }
}
