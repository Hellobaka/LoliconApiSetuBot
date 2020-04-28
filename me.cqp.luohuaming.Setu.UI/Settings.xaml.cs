using me.cqp.luohuaming.Setu.Code;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Timers;
using System.Windows.Threading;
using Native.Sdk.Cqp.Model;
using System.Threading;
using MaterialDesignThemes.Wpf;


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
            string path = CQSave.AppDirectory + @"\Config.ini";
            INIhelper.IniWrite("Config", "ApiSwitch", togglebutton_ApiKey.IsChecked.GetValueOrDefault() ? "1" : "0", path);
            if (togglebutton_ApiKey.IsChecked.GetValueOrDefault())
            {
                INIhelper.IniWrite("Config", "ApiKey", textbox_ApiKey.Text, path);
            }
            INIhelper.IniWrite("Config", "MaxofPerson", (IsPureInteger(textbox_PersonLimit.Text)) ? textbox_PersonLimit.Text : "5", path);
            INIhelper.IniWrite("Config", "MaxofGroup", (IsPureInteger(textbox_GroupLimit.Text)) ? textbox_GroupLimit.Text : "30", path);
            int count=0;
            List<BindingGroup> group =(List<BindingGroup>) ItemControl_Group.DataContext;
            foreach(var item in group)
            {
                if (item.IsChecked)
                {                    
                    INIhelper.IniWrite("GroupList", $"Index{count}", item.GroupId.ToString(), path);
                    count++;                
                }
            }
            INIhelper.IniWrite("GroupList", "Count", count.ToString(), path);
            count = 0;
            foreach (var item in listbox_Admin.Items)
            {                
                INIhelper.IniWrite("Admin", $"Index{count}", item.ToString(), path);
                count++;
            }
            INIhelper.IniWrite("Admin", "Count", count.ToString(), path);
            SnackbarMessage_Show("更改已保存", 2);
        }
        /// <summary>
        /// 底部提示条消息显示
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="seconds">存留的秒数</param>
        public void SnackbarMessage_Show(string message, double seconds)
        {
            parentwindow.Xamldisplay_Snackbar.Visibility = Visibility.Visible;
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
            string path = CQSave.AppDirectory + @"\Config.ini";
            togglebutton_ApiKey.IsChecked = INIhelper.IniRead("Config", "ApiSwitch", "0", CQSave.AppDirectory + @"\Config.ini") == "1" ? true : false;
            textbox_ApiKey.Text = INIhelper.IniRead("Config", "ApiKey", "0", CQSave.AppDirectory + @"\Config.ini");
            textbox_PersonLimit.Text = INIhelper.IniRead("Config", "MaxofPerson", "5", path);
            textbox_GroupLimit.Text = INIhelper.IniRead("Config", "MaxofGroup", "30", path);

            int count = INIhelper.IniRead("Admin", "Count", "0", path).ToInt32();
            for(int i=0;i<count;i++)
            {
                listbox_Admin.Items.Add(INIhelper.IniRead("Admin", $"Index{i}", "0", path));
            }

            var groups = CQSave.cq.GetGroupList();
            List<BindingGroup> group = new List<BindingGroup>();
            try
            {
                foreach (var item in groups)
                {
                    BindingGroup temp = new BindingGroup();
                    temp.IsChecked = CheckGroupOpen(item.Group.Id);
                    temp.GroupName = item.Group.GetGroupInfo().Name;
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
            //List<BindingGroup> group = new List<BindingGroup>();
            //for (int i = 0; i < 20; i++)
            //{
            //    Thread.Sleep(10);
            //    BindingGroup temp = new BindingGroup();
            //    Random rd = new Random();
            //    temp.IsChecked = rd.Next(0, 2) == 1 ? true : false;
            //    temp.GroupName = rd.Next().ToString();
            //    temp.GroupId = rd.Next();
            //    group.Add(temp);
            //}
            ItemControl_Group.DataContext = group;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(GetImageFloderInfo);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }
        public bool CheckGroupOpen(long groupid)
        {
            string path = CQSave.AppDirectory + @"\Config.ini";
            int count = INIhelper.IniRead("GroupList", "Count", "0", path).ToInt32();
            for(int i=0;i<count;i++)
            {
                if(groupid==INIhelper.IniRead("GroupList",$"Index{i}","0",path).ToInt64())
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
            if (!Directory.Exists(CQSave.ImageDirectory + "LoliconPic"))
            {
                Directory.CreateDirectory(CQSave.ImageDirectory + "LoliconPic");
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(CQSave.ImageDirectory + "LoliconPic");
            FileInfo[] fileInfo = directoryInfo.GetFiles();
            double filesize = 0, count = 0;
            foreach (var item in fileInfo)
            {
                count++;
                filesize += item.Length;
            }
            textblock_Size.Text = $"{(filesize / 1048576).ToString("0.00")} MB";
            textblock_Count.Text = $"{count} 个";
        }

        private void button_ClearCache_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button_OpenFloder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(CQSave.ImageDirectory + "LoliconPic"));
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
            if (!long.TryParse(textbox_Admin.Text, out temp)) {
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
            if(e.Key==Key.Delete)
            {
                listbox_Admin.Items.RemoveAt(listbox_Admin.SelectedIndex);
                if(listbox_Admin.Items.Count==0)
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
            if(e.Key==Key.Enter)
            {
                button_Plus_Click(sender, e);
            }
        }

        private void DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            //you can cancel the dialog close:
            //eventArgs.Cancel();
            if (eventArgs.Parameter == null)
            {
                return; 
            }
            if ((bool)eventArgs.Parameter !=true) return; 

            if (Directory.Exists(CQSave.ImageDirectory + "LoliconPic"))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(CQSave.ImageDirectory + "LoliconPic");
                FileInfo[] fileInfo = directoryInfo.GetFiles();
                foreach (var item in fileInfo)
                {
                    item.Delete();
                }
            }
            SnackbarMessage_Show("清理完成", 2);
        }

        private void button_Proxy_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
