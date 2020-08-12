using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using me.cqp.luohuaming.Setu.Code;
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
            try
            {
                if (window == null)
                {
                    Thread thread = new Thread(() =>
                    {
                        window = new MainWindow();
                        window.Closing += Window_Closing;
                        window.ShowDialog();
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();                    
                }
                else
                {
                    window.Activate();
                }
            }
            catch(Exception exc)
            {
                CQSave.cqlog.Info("Error",exc.Message,exc.StackTrace);
            }
                    //window = new MainWindow();
                    //window.Closing += Window_Closing;
                    //window.Show();           
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
