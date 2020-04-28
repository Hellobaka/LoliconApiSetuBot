using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace me.cqp.luohuaming.Setu.Code
{
    public static class INIhelper
    {
        static StringBuilder val;
        /// <summary>
        /// 修改INI配置文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">关键字</param>
        /// <param name="val">值</param>
        /// <param name="filepath">文件完整路径</param>
        /// <returns></returns>        
         [DllImport("kernel32")]
         public static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        /// <summary>
        /// 读INI配置文件
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="def">缺省值</param>
        /// <param name="retval"></param>
        /// <param name="size">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);
        /// <summary>
        /// 修改INI配置文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">关键字</param>
        /// <param name="val">值</param>
        /// <param name="filepath">文件完整路径</param>
        /// <returns></returns>    
        public static long IniWrite(string section, string key, string val, string filepath)
        {
            return WritePrivateProfileString(section, key, val, filepath);
        }
        /// <summary>
        /// 读INI配置文件 重载参数少
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">关键字</param>
        /// <param name="def">缺省值</param>
        /// <param name="filePath">完整路径</param>
        /// <returns></returns>
        public static string IniRead(string section, string key, string def, string filePath)
        {
            StringBuilder retval = new StringBuilder();
            val = retval;
            GetPrivateProfileString(section, key, def, retval, 255, filePath);
            return retval.ToString();
        }
        public static int ToInt32(this string str)
        {
            try
            {               
                return  Convert.ToInt32(str);
            }
            catch
            {
                return 0;
            }
        }
        public static long ToInt64(this string str)
        {
            try
            {
                return Convert.ToInt64(str);
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// 读INI配置文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">关键字</param>
        /// <param name="def">缺省值</param>
        /// <param name="retval">StringBuilder类成员</param>
        /// <param name="size">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="filePath">完整路径</param>
        /// <returns></returns>
        public static long IniRead(string section, string key, string def, StringBuilder retval, int size,  string filePath)
        {            
            return GetPrivateProfileString(section, key, def,retval,size ,filePath);
        }
    }
}
