using me.cqp.luohuaming.Setu.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ColorZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void UIElement_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //until we had a StaysOpen glag to Drawer, this will help with scroll bars
            var dependencyObject = Mouse.Captured as DependencyObject;
            while (dependencyObject != null)
            {
                if (dependencyObject is ScrollBar) return;
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            MenuToggleButton.IsChecked = false;
        }

        private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {                
                string tag = "";
                switch (sender.GetType().Name)
                {
                    case "StackPanel":
                        StackPanel item1 = sender as StackPanel;
                        tag = item1.Tag.ToString();
                        break;
                    case "ListBoxItem":
                        ListBoxItem item2 = sender as ListBoxItem;
                        tag = item2.Tag.ToString();
                        break;
                    case "TextBlock":
                        TextBlock item3 = sender as TextBlock;
                        tag = item3.Tag.ToString();
                        break;
                }
                //this.frmMain.Navigate(new Uri("pack://application:,,,/me.cqp.luohuaming.Setu.UI;component/" + tag + ".xaml", UriKind.Absolute));
                if (tag == "Settings")
                {
                    if (frmMain.Content.GetType().Name == "Settings") return;
                    Settings pg = new Settings();
                    frmMain.Content = pg;
                    pg.parentwindow = this;
                }
                else if (tag == "AboutMe")
                {
                    if (frmMain.Content.GetType().Name == "AboutMe") return;
                    AboutMe pg = new AboutMe();
                    frmMain.Content = pg;
                    pg.parentwindow = this;
                }
                else if (tag == "CustomAPI")
                {
                    if (frmMain.Content.GetType().Name == "CustomAPI") return;
                    CustomAPI pg = new CustomAPI();
                    frmMain.Content = pg;
                    pg.parentwindow = this;
                }
            }
            catch(Exception exc)
            {
                Console.WriteLine(exc.Message + " " + exc.Source);
            }
        }

        public void PagesTurn(string pagename)
        {
            try
            {
                if (pagename == "Back")
                {
                    frmMain.GoBack();
                }
                //this.frmMain.Navigate(new Uri("pack://application:,,,/me.cqp.luohuaming.Setu.UI;component/" + pagename + ".xaml", UriKind.Absolute));
            }
            catch(Exception e)
            {
                SnackbarMessage_Show($"发生错误:{e.Message}", 2);
            }

        }
        /// <summary>
        /// 底部提示条消息显示
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="seconds">存留的秒数</param>
        public void SnackbarMessage_Show(string message, double seconds)
        {
            Snackbar_Message.Visibility = Visibility.Visible;
            Snackbar_Message.MessageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(seconds));
            //Xamldisplay_Snackbar.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Settings pg = new Settings();
            frmMain.Content = pg;
            pg.parentwindow = this;
        }
    }
}
