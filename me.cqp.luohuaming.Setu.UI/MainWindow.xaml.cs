using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace me.cqp.luohuaming.Setu.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; set; }

        public bool FormLoaded { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
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

        /// <summary>
        /// 底部提示条消息显示
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="seconds">存留的秒数</param>
        public void SnackbarMessage_Show(string message, double seconds)
        {
            Snackbar_Message.Visibility = Visibility.Visible;
            Snackbar_Message.MessageQueue.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(seconds));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Settings pg = new Settings();
            frmMain.Content = pg;
            pg.ParentWindow = this;
            FormLoaded = true;
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!FormLoaded) return;
            string name = ((ListBoxItem)MenuListBox.SelectedItem).Tag.ToString();
            switch (name)
            {
                case "Settings":
                    if (frmMain.Content.GetType().Name == "Settings") return;
                    frmMain.Content = new Settings();
                    break;
                case "CustomAPI":
                    if (frmMain.Content.GetType().Name == "CustomAPI") return;
                    frmMain.Content = new CustomAPI();
                    break;
                case "LocalPic":
                    if (frmMain.Content.GetType().Name == "LocalPic") return;
                    frmMain.Content = new LocalPic();
                    break;
                case "JsonDeserize":
                    if (frmMain.Content.GetType().Name == "JsonDeserize") return;
                    frmMain.Content = new JsonDeserize();
                    break;
                case "AboutMe":
                    if (frmMain.Content.GetType().Name == "AboutMe") return;
                    frmMain.Content = new AboutMe();
                    break;
                default:
                    break;
            }
        }
    }
}
