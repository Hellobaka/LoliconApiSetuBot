using Native.Tool.Http;
using Newtonsoft.Json;
using PublicInfos;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace me.cqp.luohuaming.Setu.UI
{
    public class Update
    {
        public string SetuVersion { get; set; }
        public string Date { get; set; }
        public string Whatsnew { get; set; }
    }
    /// <summary>
    /// AboutMe.xaml 的交互逻辑
    /// </summary>
    public partial class AboutMe : Page
    {
        public AboutMe()
        {
            InitializeComponent();
        }

        MainWindow _parentWin;
        public MainWindow parentwindow
        {
            get { return _parentWin; }
            set { _parentWin = value; }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.Tag.ToString()));
        }
        private void Btn_CkUpdate_Click(object sender, RoutedEventArgs e)
        {                
            progressbar_update.Visibility = Visibility.Visible;

            string update = Encoding.UTF8.GetString(HttpWebClient.Get("https://gitee.com/Hellobaka/LoliconApiSetuBot/raw/master/New.json"));
            var updateinfo = JsonConvert.DeserializeObject<Update>(update);
            progressbar_update.Visibility = Visibility.Hidden;
            ShowUpdateContent(updateinfo);

        }
        private void ShowUpdateContent(Update updateinfo)
        {
            bool isnew = updateinfo.SetuVersion != MainSave.CQApi.AppInfo.Version.ToString();

            StackPanel stackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Margin = new Thickness(10, 10, 10, 10);
            StackPanel panel = new StackPanel();
            panel.MinWidth = 400;
            TextBlock text_Title = new TextBlock
            {
                Text = isnew ? "发现新版本" : "暂无新版本",
                FontSize = 25,
            };                
            panel.Children.Add(text_Title);
            Thickness thickness = new Thickness(5);
            if (isnew)
            {
                TextBlock text_Version = new TextBlock()
                {
                    Text = $"新版本:{updateinfo.SetuVersion}",
                    Margin = thickness
                };
                TextBlock text_Date = new TextBlock()
                {
                    Text = $"更新日期:{updateinfo.Date}",
                    Margin = thickness
                }; TextBlock text_Whatsnew = new TextBlock()
                {
                    Text = $"更新内容:{updateinfo.Whatsnew}",
                    Margin = thickness,                    
                };
                panel.Children.Add(text_Version);panel.Children.Add(text_Date); panel.Children.Add(text_Whatsnew);

            }
            else
            {
                TextBlock text_Msg = new TextBlock() { Text = "是最新的了呢", Margin = thickness };
                panel.Children.Add(text_Msg);
            }
            Button button = new Button
            {
                Content = "好的",
                HorizontalAlignment=HorizontalAlignment.Center,
                Width=100,
                Margin=new Thickness(0,40,0,0)
            };
            button.Click += (object sender1, RoutedEventArgs e1) => { dialog_AboutMe.IsOpen = false; };
            panel.Children.Add(button);
            stackPanel.Children.Add(panel);
            dialog_AboutMe.DialogContent = stackPanel;
            dialog_AboutMe.IsOpen = true;
        }

    }
}
