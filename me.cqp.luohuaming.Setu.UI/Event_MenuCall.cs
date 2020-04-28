using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace me.cqp.luohuaming.Setu.UI
{
    public class Event_MenuCall : IMenuCall
    {
        private MainWindow window = null;
        public void MenuCall(object sender, CQMenuCallEventArgs e)
        {

            if (window == null)
            {
                window = new MainWindow();
                window.Closing += Window_Closing;
                window.Show();
            }
            else
            {
                window.Activate();
            }
        }

        ///<summary>
        ///窗体关闭时触发
        ///</summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window = null;
        }
    }
}
