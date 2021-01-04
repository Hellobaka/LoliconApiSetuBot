using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using Native.Tool.IniConfig;
using Native.Tool.IniConfig.Linq;
using PublicInfos;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        MainWindow _parentWin;
        public MainWindow parentwindow
        {
            get { return _parentWin; }
            set { _parentWin = value; }
        }
        static IniConfig ini;
        public Settings()
        {
            InitializeComponent();
        }
        class BindingGroup
        {
            public bool IsChecked { get; set; }
            public long GroupId { get; set; }
            public string GroupName { get; set; }
        }
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.Tag.ToString()));
        }

        private void button_SettingsSave_Click(object sender, RoutedEventArgs e)
        {
            string path = MainSave.AppDirectory + @"\Config.ini";
            ini = new IniConfig(path);ini.Load();
            ini.Object["Config"]["ApiSwitch"]=new IValue(togglebutton_ApiKey.IsChecked.GetValueOrDefault() ? "1" : "0");
            if (togglebutton_ApiKey.IsChecked.GetValueOrDefault())
            {
                ini.Object["Config"]["ApiKey"]=new IValue(textbox_ApiKey.Text);
            }
            ini.Object["Config"]["MaxofPerson"]=new IValue((IsPureInteger(textbox_PersonLimit.Text)) ? textbox_PersonLimit.Text : "5");
            ini.Object["Config"]["MaxofGroup"]=new IValue((IsPureInteger(textbox_GroupLimit.Text)) ? textbox_GroupLimit.Text : "30");
            int count = 0;
            List<BindingGroup> group = (List<BindingGroup>)ItemControl_Group.DataContext;
            foreach (var item in group)
            {
                if (item.IsChecked)
                {
                    ini.Object["GroupList"][$"Index{count}"]=new IValue(item.GroupId.ToString());
                    count++;
                }
            }
            ini.Object["GroupList"]["Count"]=new IValue(count.ToString());
            count = 0;
            foreach (var item in listbox_Admin.Items)
            {
                ini.Object["Admin"][$"Index{count}"]=new IValue(item.ToString());
                count++;
            }
            ini.Object["Admin"]["Count"]=new IValue(count.ToString());
            ini.Save();
            SnackbarMessage_Show("更改已保存", 2);
        }
        /// <summary>
        /// 底部提示条消息显示
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="seconds">存留的秒数</param>
        public void SnackbarMessage_Show(string message, double seconds)
        {
            parentwindow.Snackbar_Message.Visibility = Visibility.Visible;
            parentwindow.Snackbar_Message.MessageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(seconds));
            //Xamldisplay_Snackbar.Visibility = Visibility.Collapsed;
        }

        private bool IsPureInteger(string str)
        {
            try
            {
                Convert.ToInt64(str);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private DispatcherTimer dispatcherTimer = null;
        private void Page_Settings_Loaded(object sender, RoutedEventArgs e)
        {
            string path = MainSave.AppDirectory + "Config.ini";
            ini = new IniConfig(path);
            ini.Load();
            togglebutton_ApiKey.IsChecked = ini.Object["Config"]["ApiSwitch"].GetValueOrDefault("0") == "1" ? true : false;
            textbox_ApiKey.Text = ini.Object["Config"]["ApiKey"].GetValueOrDefault("0");
            textbox_PersonLimit.Text = ini.Object["Config"]["MaxofPerson"].GetValueOrDefault("5");
            textbox_GroupLimit.Text = ini.Object["Config"]["MaxofGroup"].GetValueOrDefault("30");

            int count = ini.Object["Admin"]["Count"].GetValueOrDefault(0);
            for (int i = 0; i < count; i++)
            {
                listbox_Admin.Items.Add(ini.Object["Admin"][$"Index{i}"].GetValueOrDefault((long)0));
            }
            if(MainSave.CQApi!=null)
            {
                var groups = MainSave.CQApi.GetGroupList();
                List<BindingGroup> group = new List<BindingGroup>();
                try
                {
                    foreach (var item in groups)
                    {
                        BindingGroup temp = new BindingGroup();
                        temp.IsChecked = CheckGroupOpen(item.Group.Id);
                        temp.GroupName = item.Name;
                        temp.GroupId = item.Group.Id;
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
                ItemControl_Group.DataContext = group;
            }            

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(GetImageFloderInfo);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }
        public bool CheckGroupOpen(long groupid)
        {
            string path = MainSave.AppDirectory + @"\Config.ini";
            int count = ini.Object["GroupList"]["Count"].GetValueOrDefault(0);
            for (int i = 0; i < count; i++)
            {
                if (groupid == ini.Object["GroupList"][$"Index{i}"].GetValueOrDefault((long)0))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 更新图片文件夹信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GetImageFloderInfo(object sender, EventArgs e)
        {
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
            if(Directory.Exists(MainSave.ImageDirectory+ "CustomAPIPic"))
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
            textblock_Size.Text = $"{filesize / 1048576:0.00} MB";
            textblock_Count.Text = $"{count} 个";
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
            bt_No.Click += (object sender2, RoutedEventArgs e2) => { dialoghost_Main.IsOpen = false; };

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
            Process.Start(new ProcessStartInfo(MainSave.ImageDirectory + "LoliconPic"));
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
            long temp;
            if (!long.TryParse(textbox_Admin.Text, out temp))
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
            panel.Margin = new Thickness(16,16,16,16);
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
