using me.cqp.luohuaming.Setu.PublicInfos;
using me.cqp.luohuaming.Setu.PublicInfos.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        public MainWindow ParentWindow { get; set; }
        public class BindingGroup
        {
            public bool IsChecked { get; set; }
            public long GroupId { get; set; }
            public string GroupName { get; set; }
        }
        public Settings()
        {
            InitializeComponent();
        }
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.Tag.ToString()));
        }

        private void button_SettingsSave_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_GroupLimit.Text.ToInt() < 0 || textbox_PersonLimit.Text.ToInt() < 0)
            {
                SnackbarMessage_Show("配置项存在非纯数字", 5);
                return;
            }
            ConfigHelper.SetConfig("MaxGroupQuota", textbox_GroupLimit.Text.ToInt());
            ConfigHelper.SetConfig("MaxPersonQuota", textbox_PersonLimit.Text.ToInt());

            List<BindingGroup> group = (List<BindingGroup>)ItemControl_Group.DataContext;
            ConfigHelper.SetConfig("GroupList", group.Where(x => x.IsChecked).Select(x => x.GroupId).ToList().Join("|"));

            string str = "";
            foreach (var item in listbox_Admin.Items)
            {
                str += item.ToString() + "|";
            }
            if(str.Length > 0)
                ConfigHelper.SetConfig("AdminList", str.Substring(0, str.Length - 1));

            ConfigHelper.InitConfig();
            SnackbarMessage_Show("更改已保存", 2);
        }

        /// <summary>
        /// 底部提示条消息显示
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="seconds">存留的秒数</param>
        public void SnackbarMessage_Show(string message, double seconds)
        {
            ParentWindow.Snackbar_Message.Visibility = Visibility.Visible;
            ParentWindow.Snackbar_Message.MessageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(seconds));
        }

        private void Page_Settings_Loaded(object sender, RoutedEventArgs e)
        {
            textbox_PersonLimit.Text = AppConfig.MaxPersonQuota.ToString();
            textbox_GroupLimit.Text = AppConfig.MaxGroupQuota.ToString();

            foreach (var item in AppConfig.AdminList)
            {
                listbox_Admin.Items.Add(item);
            }
            new Thread(() =>
            {
                if (MainSave.CQApi != null)
                {
                    var groups = MainSave.CQApi.GetGroupList();
                    List<BindingGroup> group = new List<BindingGroup>();
                    try
                    {
                        foreach (var item in groups)
                        {
                            BindingGroup temp = new BindingGroup
                            {
                                IsChecked = AppConfig.GroupList.Contains(item.Group.Id),
                                GroupName = item.Name,
                                GroupId = item.Group.Id
                            };
                            group.Add(temp);
                        }
                    }
                    catch
                    {
                        BindingGroup temp = new BindingGroup();
                        temp.IsChecked = false;
                        temp.GroupName = "读取群列表失败...";
                        temp.GroupId = 0;
                        group.Add(temp);
                    }
                    Dispatcher.Invoke(() => ItemControl_Group.DataContext = group);
                }

                double filesize = 0, count = 0;
                DirectoryInfo directoryInfo;
                FileInfo[] fileInfo;
                if (Directory.Exists(MainSave.ImageDirectory + "LoliconPic"))
                {
                    directoryInfo = new DirectoryInfo(MainSave.ImageDirectory + "LoliconPic");
                    fileInfo = directoryInfo.GetFiles();
                    foreach (var item in fileInfo)
                    {
                        count++;
                        filesize += item.Length;
                    }
                }
                if (Directory.Exists(MainSave.ImageDirectory + "CustomAPIPic"))
                {
                    directoryInfo = new DirectoryInfo(MainSave.ImageDirectory + "CustomAPIPic");
                    fileInfo = directoryInfo.GetFiles();
                    foreach (var item in fileInfo)
                    {
                        count++;
                        filesize += item.Length;
                    }
                }
                if (Directory.Exists(MainSave.ImageDirectory + "JsonDeserizePic"))
                {
                    directoryInfo = new DirectoryInfo(MainSave.ImageDirectory + "JsonDeserizePic");
                    fileInfo = directoryInfo.GetFiles();
                    foreach (var item in fileInfo)
                    {
                        count++;
                        filesize += item.Length;
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    textblock_Size.Text = $"{filesize / 1048576:0.00} MB";
                    textblock_Count.Text = $"{count} 个";
                });
            }).Start();
        }

        private void button_ClearCache_Click(object sender, RoutedEventArgs e)
        {
            TextBlock text = new TextBlock();
            text.Text = "确定要删除所有缓存图片？这一过程不可逆！";

            Button bt_Yes = new Button();
            bt_Yes.Content = "GKD!";
            bt_Yes.Margin = new Thickness(0, 10, 5, 0);
            bt_Yes.Click += ClearCache;

            Button bt_No = new Button();
            bt_No.Content = "GCK!";
            bt_No.Margin = new Thickness(5, 10, 0, 0);
            bt_No.Click += (_, _) => { dialoghost_Main.IsOpen = false; };

            StackPanel panelHorizontal = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panelHorizontal.Children.Add(bt_Yes);
            panelHorizontal.Children.Add(bt_No);

            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(16, 16, 16, 16);
            panel.Children.Add(text);
            panel.Children.Add(panelHorizontal);

            dialoghost_Main.DialogContent = panel;
            dialoghost_Main.IsOpen = true;
        }

        private void ClearCache(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(MainSave.ImageDirectory + "LoliconPic"))
                Directory.Delete(MainSave.ImageDirectory + "LoliconPic", true);
            if (Directory.Exists(MainSave.ImageDirectory + "CustomAPIPic"))
                Directory.Delete(MainSave.ImageDirectory + "CustomAPIPic", true);
            if (Directory.Exists(MainSave.ImageDirectory + "JsonDeserizePic"))
                Directory.Delete(MainSave.ImageDirectory + "JsonDeserizePic", true);
            SnackbarMessage_Show("清理完成", 2);
            dialoghost_Main.IsOpen = false;
        }

        private void button_OpenFloder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(MainSave.ImageDirectory + "LoliconPic");
        }

        private void button_AllSelect_Click(object sender, RoutedEventArgs e)
        {
            List<BindingGroup> group = (List<BindingGroup>)ItemControl_Group.DataContext;
            foreach (var item in group)
            {
                item.IsChecked = true;
            }
            ItemControl_Group.DataContext = null;
            ItemControl_Group.DataContext = group;
        }

        private void button_NonSelect_Click(object sender, RoutedEventArgs e)
        {
            List<BindingGroup> group = (List<BindingGroup>)ItemControl_Group.DataContext;
            foreach (var item in group)
            {
                item.IsChecked = false;
            }
            ItemControl_Group.DataContext = null;
            ItemControl_Group.DataContext = group;
        }

        private void button_InvertSelect_Click(object sender, RoutedEventArgs e)
        {
            List<BindingGroup> group = (List<BindingGroup>)ItemControl_Group.DataContext;
            foreach (var item in group)
            {
                item.IsChecked = !item.IsChecked;
            }
            ItemControl_Group.DataContext = null;
            ItemControl_Group.DataContext = group;
        }

        private void button_Plus_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textbox_Admin.Text)) return;
            if (!long.TryParse(textbox_Admin.Text, out _))
            {
                SnackbarMessage_Show("格式错误", 2);
                textbox_Admin.Text = "";
                return;
            }
            listbox_Admin.Items.Add(textbox_Admin.Text);
            textbox_Admin.Text = "";
            listbox_Admin.SelectedIndex = listbox_Admin.Items.Count - 1;
        }

        private void listbox_Admin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                listbox_Admin.Items.RemoveAt(listbox_Admin.SelectedIndex);
                if (listbox_Admin.Items.Count == 0)
                {
                    listbox_Admin.SelectedIndex = 0;
                }
                else
                {
                    listbox_Admin.SelectedIndex = listbox_Admin.Items.Count - 1;
                }
            }
        }

        private void textbox_Admin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                button_Plus_Click(sender, e);
            }
        }

        private void ShowDialogwithPage(object sender, RoutedEventArgs e)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(16, 16, 16, 16);
            Frame fm = new Frame();
            fm.Content = GetPage((sender as MenuItem).Tag.ToString());
            panel.Children.Add(fm);
            dialoghost_Main.DialogContent = panel;
            dialoghost_Main.IsOpen = true;
        }

        Page GetPage(string tag)
        {
            Page pg;
            switch (tag)
            {
                case "SetuProxy":
                    pg = new SetuProxy();
                    break;
                case "OrderDIY":
                    pg = new OrderDIY();
                    break;
                case "ExtraConfig":
                    pg = new ExtraConfig();
                    break;
                default:
                    pg = new Page();
                    break;
            }
            return pg;
        }

    }
}
