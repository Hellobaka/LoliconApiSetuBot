using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using me.cqp.luohuaming.Setu.Code;
using Native.Tool.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// JsonDeserize.xaml 的交互逻辑
    /// </summary>
    public partial class JsonDeserize : Page
    {
        public JsonDeserize()
        {
            InitializeComponent();
        }
        #region ---字段---
        MainWindow _parentWin;
        public MainWindow parentwindow
        {
            get { return _parentWin; }
            set { _parentWin = value; }
        }
        public static List<JsonToDeserize> JsonSaves = new List<JsonToDeserize>();
        #endregion

        private void btn_Plus_Click(object sender, RoutedEventArgs e)
        {
            //父容器StackPanel
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;
            stackPanel.Margin = new Thickness(0, 5, 0, 5);
            //向此父容器添加控件
            foreach (var item in GetTemplateList())
            {
                stackPanel.Children.Add(item);
            }
            //向顶级容器添加父容器以完成控件的动态添加
            StackPanel_JsonMain.Children.Add(stackPanel);
        }
        /// <summary>
        /// 生成控件模板
        /// </summary>
        /// <returns></returns>
        private List<Control> GetTemplateList()
        {
            List<Control> ls = new List<Control>();
            //统一Margin
            Thickness thickness = new Thickness(10, 0, 0, 0);
            //CheckBox
            CheckBox ckb = new CheckBox();
            ls.Add(ckb);
            //ToggleButton
            ToggleButton tb = new ToggleButton();
            tb.Margin = thickness;
            ls.Add(tb);
            //三个TextBox
            TextBox text1 = new TextBox
            {
                Margin = thickness,
                MinWidth = 100,
                Text = "指令...",
                Tag = "指令...",
                Opacity = 0.6
            };
            text1.GotFocus += TextBox_FocusChanged;
            text1.LostFocus += TextBox_FocusChanged;
            TextBox text2 = new TextBox
            {
                Margin = thickness,
                MinWidth = 200,
                Text = "接口网址",
                Tag = "接口网址",
                Opacity = 0.6
            };
            text2.GotFocus += TextBox_FocusChanged;
            text2.LostFocus += TextBox_FocusChanged;
            ls.Add(text1); ls.Add(text2);
            //CheckBox
            CheckBox checkBox2 = new CheckBox();
            checkBox2.Margin = thickness;
            checkBox2.Content = "自动撤回";
            ls.Add(checkBox2);
            //Button
            Button bt = new Button()
            {
                Content = "测试",
                Height = 25,
                Margin = thickness
            };
            bt.Click += btn_Test_Click;
            ls.Add(bt);
            Button bt2 = new Button()
            {
                Content = "详细设置",
                Height = 25,
                Margin = thickness
            };
            bt2.Click += btn_Option_Click;
            ls.Add(bt2);
            return ls;
        }
        //用于显示类Hint提示
        private void TextBox_FocusChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox).IsFocused)
            {
                //如果有焦点,但内容并不是需要表现Hint的则忽略
                if ((sender as TextBox).Text != (sender as TextBox).Tag.ToString()) return;
                (sender as TextBox).Text = "";
                (sender as TextBox).Opacity = 1;
            }
            else
            {
                //失去焦点但文本不是空的,就忽略
                if (!string.IsNullOrEmpty((sender as TextBox).Text)) return;
                (sender as TextBox).Text = (sender as TextBox).Tag.ToString();
                (sender as TextBox).Opacity = 0.6;
            }
        }
        /// <summary>
        /// 去除选中的项目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Minus_Click(object sender, RoutedEventArgs e)
        {
            //由于在删的过程中,迭代器的目标会发生变化,需要使用侧门敲击的方式删除元素
            List<UIElement> ls = new List<UIElement>();
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                //item是子项目的StackPanel容器,第0个Child是CheckBox
                var child = (item as StackPanel).Children[0];
                //选中了
                if ((bool)(child as CheckBox).IsChecked)
                {
                    ls.Add(item);
                }
            }
            if (ls.Count == 0)
            {
                parentwindow.SnackbarMessage_Show("未选中任何控件，点击滑块前的方块选择控件", 1.2);
                return;
            }
            foreach (UIElement item in ls)
            {
                StackPanel_JsonMain.Children.Remove(item);
            }
        }

        private static Control PicFolderButton;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PicFolderButton = ((Control)(StackPanel_JsonMain.Children[0] as StackPanel).Children[4]);
            if (!File.Exists(CQSave.AppDirectory + "JsonDeserize.json")) return;
            string temp = File.ReadAllText(CQSave.AppDirectory + "JsonDeserize.json");
            //反序列化
            JsonSaves = JsonConvert.DeserializeObject<List<JsonToDeserize>>(temp);
            //读取到了内容,为了写内容方便,先清空内容
            if (JsonSaves.Count != 0)
            {
                StackPanel_JsonMain.Children.Clear();
            }
            for (int i = 0; i < JsonSaves.Count; i++)
            {
                //先加模板
                btn_Plus_Click(sender, e);
                //再对元素内容进行修改
                var child = StackPanel_JsonMain.Children[i] as StackPanel;
                (child.Children[1] as ToggleButton).IsChecked = JsonSaves[i].Enabled;
                (child.Children[2] as TextBox).Text = JsonSaves[i].Order;
                (child.Children[3] as TextBox).Text = JsonSaves[i].url;
                (child.Children[4] as CheckBox).IsChecked = JsonSaves[i].AutoRevoke;
            }
        }
        /// <summary>
        /// 测试接口是否能用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            //获取按钮的父容器以获取Path元素
            var parent = VisualTreeHelper.GetParent((UIElement)sender);
            string url = ((TextBox)VisualTreeHelper.GetChild(parent, 3)).Text;
            //Path元素在父容器中是[4]个
            string path = JsonSaves.Find(x=>x.url==url).picPath;
            try
            {
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = CQSave.proxy,
                    AllowAutoRedirect = true,
                })
                {
                    string json = Encoding.UTF8.GetString(http.DownloadData(url)).Replace('﻿', ' ');

                    string picpath = CQSave.ImageDirectory + "\\JsonDeserizePic\\" + Guid.NewGuid() + ".png";
                    if (!Directory.Exists(CQSave.ImageDirectory + "\\JsonDeserizePic"))
                        Directory.CreateDirectory(CQSave.ImageDirectory + "\\JsonDeserizePic");
                    JObject jObject = JObject.Parse(json);
                    url = jObject.SelectToken(path).ToString();

                    http.CookieCollection = new System.Net.CookieCollection();
                    http.DownloadFile(url, picpath);
                    parentwindow.SnackbarMessage_Show($"接口测试通过", 1);
                    Process.Start(picpath);
                }
            }
            catch(Exception exc)
            {
                CQSave.cqlog.Info("Json解析", exc.Message, exc.StackTrace);
                parentwindow.SnackbarMessage_Show($"接口测试失败,查看日志获取详细信息", 1);
            }
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            List<JsonToDeserize> ls = new List<JsonToDeserize>();
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                //判断是否存在填了链接但是没有填指令的情况
                if (((item as StackPanel).Children[3] as TextBox).Text != "接口网址" &&
                    ((item as StackPanel).Children[2] as TextBox).Text == "指令...")
                {
                    parentwindow.SnackbarMessage_Show("存在一行路径已设置但指令未设置，请纠正", 1);
                    return;
                }
                if (((item as StackPanel).Children[3] as TextBox).Text == "接口网址" && ((item as StackPanel).Children[2] as TextBox).Text == "指令...")
                {
                    continue;
                }
                //不是空
                if (((item as StackPanel).Children[3] as TextBox).Text != "接口网址"
                    && ((item as StackPanel).Children[2] as TextBox).Text != "指令...")
                {
                    JsonToDeserize save = new JsonToDeserize
                    {
                        Enabled = (bool)((item as StackPanel).Children[1] as ToggleButton).IsChecked,
                        Order = ((item as StackPanel).Children[2] as TextBox).Text,
                        url = ((item as StackPanel).Children[3] as TextBox).Text,
                        AutoRevoke = (bool)((item as StackPanel).Children[4] as CheckBox).IsChecked,
                    };
                    save.picPath = JsonSaves.Find(x => x.url == save.url).picPath;
                    save.Text = JsonSaves.Find(x => x.url == save.url).Text;
                    ls.Add(save);
                }
            }
            //序列化
            string temp = JsonConvert.SerializeObject(ls);
            File.WriteAllText(CQSave.AppDirectory + "JsonDeserize.json", temp);
            parentwindow.SnackbarMessage_Show("设置已保存", 1);
        }
        //按ListBoxItem事件
        private void ListBoxItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switch ((sender as ListBoxItem).Tag)
            {
                case "clearall":
                    ClearAll();
                    break;
                case "allcheck":
                    AllCheck();
                    break;
                case "allenabled":
                    AllEnabled();
                    break;
                case "alldisabled":
                    AllDisabled();
                    break;
                case "alltest":
                    AllTest();
                    break;
            }
        }
        #region --事件相应方法--
        void ClearAll()
        {
            StackPanel_JsonMain.Children.Clear();
        }
        void AllCheck()
        {
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                ((item as StackPanel).Children[0] as CheckBox).IsChecked = true;
            }
        }
        void AllEnabled()
        {
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                var child = ((item as StackPanel).Children[1] as ToggleButton);
                child.IsChecked = true;
            }

        }
        void AllDisabled()
        {
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                var child = ((item as StackPanel).Children[1] as ToggleButton);
                child.IsChecked = false;
            }

        }
        void AllTest()
        {
            foreach (UIElement item in StackPanel_JsonMain.Children)
            {
                btn_Test_Click(((item as StackPanel).Children[7] as Button), new RoutedEventArgs());
            }
        }
        #endregion

        private void btn_Option_Click(object sender, RoutedEventArgs e)
        {
            JsonSettings pg = new JsonSettings();
            var parent = VisualTreeHelper.GetParent((UIElement)sender);
            string url = ((TextBox)VisualTreeHelper.GetChild(parent, 3)).Text;
            pg.Json_Object = JsonSaves.Find(x => x.url == url);
            ShowDialogwithPage(pg);
        }
        private void ShowDialogwithPage(Page page)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(10);
            Frame fm = new Frame();
            fm.Content = page;
            panel.Children.Add(fm);
            DialogHost_JsonMain.DialogContent = panel;
            DialogHost_JsonMain.IsOpen = true;
        }
    }
}
