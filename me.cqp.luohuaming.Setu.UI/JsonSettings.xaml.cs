using Native.Tool.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicInfos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// JsonSettings.xaml 的交互逻辑
    /// </summary>
    public partial class JsonSettings : Page
    {
        public JsonSettings()
        {
            InitializeComponent();
        }        
        public JsonToDeserize Json_Object;
        static JObject Json_Main;

        private void TreeView_Json_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = (TreeViewItem)(sender as TreeView).SelectedItem;
            if (item == null)
                return;
            TextBox_Path.Text = item.Tag.ToString();
            JToken jToken = Json_Main.SelectToken(item.Tag.ToString());
            Dg(jToken, item);
        }

        private void Button_Get_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox_URL.Text))
            {
                Snackbar_ShowMessage($"网址栏不可为空", 1);
                return;
            }
            try
            {
                using (HttpWebClient http = new HttpWebClient()
                {
                    TimeOut = 10000,
                    Encoding = Encoding.UTF8,
                    Proxy = MainSave.Proxy,
                    AllowAutoRedirect = true,
                })
                {
                    string url = TextBox_URL.Text;
                    Thread thread=new Thread(() =>
                    {
                        TextBox_JsonText.Dispatcher.Invoke(() =>{progressbar_Main.Visibility = Visibility.Visible;});
                        string json = Encoding.UTF8.GetString(http.DownloadData(url));
                        TextBox_JsonText.Dispatcher.Invoke(() => { TextBox_JsonText.Text = json.Replace("\ufeff", ""); });
                        TextBox_JsonText.Dispatcher.Invoke(() => {progressbar_Main.Visibility = Visibility.Hidden;});
                    });thread.Start();
                }
            }
            catch (Exception exc)
            {
                Snackbar_ShowMessage($"发生错误，错误信息:{exc.Message}", 2);
            }
        }
        private void Snackbar_ShowMessage(string message, double seconds)
        {
            SnackBar_Json.Message = new MaterialDesignThemes.Wpf.SnackbarMessage();
            SnackBar_Json.Message.Content = message;
            SnackBar_Json.IsActive = true;
            SnackBar_Json.Dispatcher.Invoke(() => { Thread.Sleep((int)(seconds * 1000));   SnackBar_Json.IsActive = false; });
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox_URL.Text = Json_Object.url;
            TextBox_PicPath.Text = Json_Object.picPath;
            TextBox_TextToSend.Text = Json_Object.Text;
        }

        private void Button_Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("若文本不为空，发送文字之后才会发送图片\n发送文本使用变量:<JsonPath> 例如: <text> \n具体jsonpath可以获取json，解析之后，点击需要解析的元素获取Path");
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            var c = JsonDeserize.JsonSaves.Find(x => x.url == TextBox_URL.Text);
            c = Json_Object;
        }
        private TreeViewItem GetItemTemplate(string text, string tag)
        {
            TreeViewItem item = new TreeViewItem() { Tag = tag };
            TextBlock textBlock = new TextBlock { Text = text };
            item.Header = textBlock;
            return item;
        }
        private void Button_Deserize_Click(object sender, RoutedEventArgs e)
        {
            TreeView_Json.Items.Clear();
            ItemList.Clear();
            try
            {
                if (string.IsNullOrEmpty(TextBox_JsonText.Text))
                {
                    Snackbar_ShowMessage("请先获取一份json", 2);
                    return;
                }
                Json_Main = (JObject)JsonConvert.DeserializeObject(TextBox_JsonText.Text);
                foreach (var item in Json_Main)
                {
                    TreeView_Json.Items.Add(GetItemTemplate(item.Key, item.Value.Path));
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"解析错误，错误信息:{exc.Message} {exc.StackTrace}");
            }
        }
        class Treeitem
        {
            public string tag { get; set; }
            public string text { get; set; }
        }
        List<Treeitem> ItemList = new List<Treeitem>();
        /// <summary>
        /// 递归读取叶节点
        /// </summary>
        /// <param name="jToken"></param>
        /// <param name="treeViewItem"></param>
        void Dg(JToken jToken, TreeViewItem treeViewItem)
        {
            if (jToken.Type == JTokenType.Array)
            {
                int count = 0;
                foreach (var item in (JArray)jToken)
                {
                    var ls = Find(count.ToString());
                    foreach (var item2 in ls)
                    {
                        if (item2.tag == item.Path)
                            return;
                    }
                    ItemList.Add(new Treeitem { tag = item.Path, text = count.ToString() });
                    TreeViewItem treeNodetemp = GetItemTemplate(count.ToString(), item.Path);
                    //treeViewItem.IsSelected = true;
                    treeViewItem.Items.Add(treeNodetemp);
                    Dg(item, treeNodetemp);
                    count++;
                }
            }
            else if (jToken.Type == JTokenType.Object)
            {
                foreach (var item in jToken)
                {
                    Dg(item, treeViewItem);
                }
            }
            else
            {
                if (jToken.Type == JTokenType.Property)
                {
                    JProperty jProperty = jToken as JProperty;
                    var ls = Find(jProperty.Name);
                    foreach (var item in ls)
                    {
                        if (item.tag == jProperty.Value.Path)
                            return;
                    }
                    ItemList.Add(new Treeitem { tag = jProperty.Value.Path, text = jProperty.Name });
                    //treeViewItem.IsSelected = true;
                    treeViewItem.Items.Add(GetItemTemplate(jProperty.Name, jProperty.Value.Path));
                }
                else
                {
                    var ls = Find(jToken.ToString());
                    foreach (var item in ls)
                    {
                        if (item.tag == jToken.Path)
                            return;
                    }
                    ItemList.Add(new Treeitem { tag = jToken.Path, text = jToken.ToString() });
                    //treeViewItem.IsSelected = true;
                    treeViewItem.Items.Add(GetItemTemplate(jToken.ToString(), jToken.Path));
                }
            }
        }
        List<Treeitem> Find(string text)
        {
            return ItemList.Where(x => x.text == text).ToList();
        }

        private void TextBox_PicPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Json_Object.picPath = TextBox_PicPath.Text;
        }

        private void TextBox_TextToSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            Json_Object.Text = TextBox_TextToSend.Text;
        }
    }
}
