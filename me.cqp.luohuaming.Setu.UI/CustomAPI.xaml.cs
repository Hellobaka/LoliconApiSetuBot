﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Native.Tool.Http;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using me.cqp.luohuaming.Setu.PublicInfos;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// CustomAPI.xaml 的交互逻辑
    /// </summary>
    public partial class CustomAPI : Page
    {
        public CustomAPI()
        {
            InitializeComponent();
        }

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
            StackPanel_Main.Children.Add(stackPanel);
        }
        /// <summary>
        /// 生成控件模板
        /// </summary>
        /// <returns></returns>
        private List<Control> GetTemplateList()
        {
            //模板依次为 CheckBox,ToggleButton,TextBox,TextBox,TextBox,CheckBox,CheckBox,Button
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
                MinWidth = 250,
                Text = "API链接",
                Tag = "API链接",
                Opacity = 0.6
            };
            text2.GotFocus += TextBox_FocusChanged;
            text2.LostFocus += TextBox_FocusChanged;
            TextBox text3 = new TextBox
            {
                Margin = thickness,
                MinWidth = 100,
                Text = "备注...",
                Tag = "备注...",
                Opacity = 0.6
            };
            text3.GotFocus += TextBox_FocusChanged;
            text3.LostFocus += TextBox_FocusChanged;
            ls.Add(text1); ls.Add(text2); ls.Add(text3);
            //CheckBox
            CheckBox checkBox = new CheckBox();
            checkBox.Margin = thickness;
            checkBox.Content = "是否限制调用次数";
            ls.Add(checkBox);
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
            foreach (UIElement item in StackPanel_Main.Children)
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
                MainWindow.Instance.SnackbarMessage_Show("未选中任何控件，点击滑块前的方块选择控件", 1.2);
                return;
            }
            foreach (UIElement item in ls)
            {
                StackPanel_Main.Children.Remove(item);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(MainSave.AppDirectory + "CustomAPI.json")) return;
            string temp = File.ReadAllText(MainSave.AppDirectory + "CustomAPI.json");
            //反序列化
            List<CustomObject> ls = JsonConvert.DeserializeObject<List<CustomObject>>(temp);
            //读取到了内容,为了写内容方便,先清空内容
            if (ls.Count != 0)
            {
                StackPanel_Main.Children.Clear();
            }
            for (int i = 0; i < ls.Count; i++)
            {
                //先加模板
                btn_Plus_Click(sender, e);
                //再对元素内容进行修改
                var child = StackPanel_Main.Children[i] as StackPanel;
                (child.Children[1] as ToggleButton).IsChecked = ls[i].Enabled;
                (child.Children[2] as TextBox).Text = ls[i].Order;
                (child.Children[3] as TextBox).Text = ls[i].URL;
                (child.Children[4] as TextBox).Text = ls[i].Remark;
                (child.Children[5] as CheckBox).IsChecked = ls[i].Usestrict;
                (child.Children[6] as CheckBox).IsChecked = ls[i].AutoRevoke;
            }
        }
        /// <summary>
        /// 测试接口是否能用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            //获取按钮的父容器以获取URL元素
            var parent = VisualTreeHelper.GetParent((UIElement)sender);
            //URL元素在父容器中是[3]个
            var textURL = (TextBox)VisualTreeHelper.GetChild(parent, 3);
            string url = textURL.Text;
            try
            {
                HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                };
                //以后要用的路径,先生成一个
                string targetdir = Path.Combine(Environment.CurrentDirectory, "data", "image", "CustomAPIPic");
                if (!Directory.Exists(targetdir))
                {
                    Directory.CreateDirectory(targetdir);
                }
                //将字节数组转换为图片
                string fullpath = Path.Combine(targetdir, DateTime.Now.ToString("yyyyMMddHHss") + ".jpg");
                http.DownloadFile(url, fullpath);
                MainWindow.Instance.SnackbarMessage_Show($"接口测试通过,图片已成功保存", 1);
                Process.Start(fullpath);
            }
            catch
            {
                MainWindow.Instance.SnackbarMessage_Show($"接口测试失败,检查接口是否有误", 1);
            }
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            List<CustomObject> ls = new List<CustomObject>();
            foreach (UIElement item in StackPanel_Main.Children)
            {
                //判断是否存在填了链接但是没有填指令的情况
                if (((item as StackPanel).Children[3] as TextBox).Text != "API链接" &&
                    ((item as StackPanel).Children[2] as TextBox).Text == "指令...")
                {
                    MainWindow.Instance.SnackbarMessage_Show("存在一行链接已设置但指令未设置，请纠正", 1);
                    return;
                }
                if (((item as StackPanel).Children[3] as TextBox).Text == "API链接" && ((item as StackPanel).Children[2] as TextBox).Text == "指令...")
                {
                    continue;
                }
                //不是空
                if (((item as StackPanel).Children[3] as TextBox).Text != "API链接" && ((item as StackPanel).Children[2] as TextBox).Text != "指令...")
                {
                    CustomObject save = new CustomObject
                    {
                        Enabled = (bool)((item as StackPanel).Children[1] as ToggleButton).IsChecked,
                        Order = ((item as StackPanel).Children[2] as TextBox).Text,
                        URL = ((item as StackPanel).Children[3] as TextBox).Text,
                        Remark = ((item as StackPanel).Children[4] as TextBox).Text,
                        Usestrict= (bool)((item as StackPanel).Children[5] as CheckBox).IsChecked,
                        AutoRevoke = (bool)((item as StackPanel).Children[6] as CheckBox).IsChecked,
                    };
                    ls.Add(save);
                }
            }
            //序列化
            string temp = JsonConvert.SerializeObject(ls);
            File.WriteAllText(MainSave.AppDirectory + "CustomAPI.json", temp);
            MainWindow.Instance.SnackbarMessage_Show("设置已保存", 1);
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
                case "noncheck":
                    NonCheck();
                    break;
                case "anticheck":
                    AntiCheck();
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
        /// <summary>
        /// 验证指令是否符合文件夹命名规则
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool OrderValidation(string order)
        {
            if (order.Length > 255)
            {
                MainWindow.Instance.SnackbarMessage_Show("指令长度不可大于255", 1.5);
                return false;
            }
            if (order.Contains("\\") ||
               order.Contains("/") ||
               order.Contains(":") ||
               order.Contains("*") ||
               order.Contains("?") ||
               order.Contains("\"") ||
               order.Contains("<") ||
               order.Contains(">") ||
               order.Contains("|"))
            {
                MainWindow.Instance.SnackbarMessage_Show("指令中不能使用字符/、\\、:、*、?、\"、<、>、| ", 1.5);
                return false;
            }
            return true;
        }

        #region --事件相应方法--
        void ClearAll()
        {
            StackPanel_Main.Children.Clear();
        }
        void AllCheck()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                ((item as StackPanel).Children[0] as CheckBox).IsChecked = true;
            }
        }
        void NonCheck()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                ((item as StackPanel).Children[0] as CheckBox).IsChecked = false;
            }
        }
        void AntiCheck()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                var child = ((item as StackPanel).Children[0] as CheckBox);
                child.IsChecked = !child.IsChecked;
            }
        }
        void AllEnabled()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                var child = ((item as StackPanel).Children[1] as ToggleButton);
                child.IsChecked = true;
            }

        }
        void AllDisabled()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                var child = ((item as StackPanel).Children[1] as ToggleButton);
                child.IsChecked = false;
            }

        }
        void AllTest()
        {
            foreach (UIElement item in StackPanel_Main.Children)
            {
                btn_Test_Click(((item as StackPanel).Children[7] as Button), new RoutedEventArgs());
            }
        }
        #endregion
    }
}
