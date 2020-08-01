﻿using Masuit.Tools.Hardware;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static System.String;

namespace Masuit.Tools.Win32
{
    /// <summary>
    /// Windows系统的系列方法
    /// </summary>
    public static class Windows
    {
        /// <summary>  
        /// 跨平台调用C++的方法
        /// </summary>
        /// <param name="hwProc">程序句柄</param>
        /// <returns></returns>
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        /// <summary>
        /// 清理系统内存，返回优化内存后的内存占用率
        /// </summary>
        /// <returns>优化内存后的内存占用率</returns>
        public static double ClearMemory()
        {
            ClearMemorySilent();
            Thread.Sleep(1000);
            return SystemInfo.GetRamInfo().MemoryUsage;
        }

        /// <summary>
        /// 静默清理系统内存
        /// </summary>
        public static void ClearMemorySilent()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            foreach (var p in Process.GetProcesses())
            {
                using (p)
                {
                    if ((p.ProcessName.Equals("System")) && (p.ProcessName.Equals("Idle")))
                    {
                        //两个系统的关键进程，不整理
                        continue;
                    }

                    try
                    {
                        EmptyWorkingSet(p.Handle);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        /// <summary>  
        /// 运行一个控制台程序并返回其输出参数。  
        /// </summary>  
        /// <param name="filename">程序名</param>  
        /// <param name="arguments">输入参数</param>
        /// <param name="recordLog">是否在控制台输出日志</param>
        /// <returns></returns>  
        public static string RunApp(string filename, string arguments, bool recordLog)
        {
            try
            {
                if (recordLog)
                {
                    Trace.WriteLine(filename + " " + arguments);
                }

                using var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = filename,
                        CreateNoWindow = true,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                proc.Start();

                using var sr = new System.IO.StreamReader(proc.StandardOutput.BaseStream, Encoding.Default);
                //上面标记的是原文，下面是我自己调试错误后自行修改的  
                Thread.Sleep(100); //貌似调用系统的nslookup还未返回数据或者数据未编码完成，程序就已经跳过直接执行  
                if (!proc.HasExited) //在无参数调用nslookup后，可以继续输入命令继续操作，如果进程未停止就直接执行  
                {
                    proc.Kill();
                }

                string txt = sr.ReadToEnd();
                if (recordLog)
                {
                    Trace.WriteLine(txt);
                }

                return txt;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return ex.Message;
            }
        }

        /// <summary>
        /// 获取操作系统版本
        /// </summary>
        /// <returns></returns>
        public static string GetOsVersion()
        {
            try
            {
                return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("ProductName").ToString();
            }
            catch (Exception)
            {
                return "未能获取到操作系统版本，可能是当前程序无管理员权限，如果是web应用程序，请将应用程序池的高级设置中的进程模型下的标识设置为：LocalSystem；如果是普通桌面应用程序，请提升管理员权限后再操作。";
            }
        }
    }

    /// <summary>
    /// 服务器信息
    /// </summary>
    public class WindowsServer
    {
        /// <summary>
        /// CPUID
        /// </summary>
        public string CpuId; //CPU的ID

        /// <summary>
        /// CPU插槽数
        /// </summary>
        public int CpuCount; //CPU的个数

        /// <summary>
        /// CPU主频
        /// </summary>
        public string[] CpuMhz; //CPU频率  单位：hz

        /// <summary>
        /// mac地址
        /// </summary>
        public string MacAddress; //计算机的MAC地址

        /// <summary>
        /// 硬盘ID
        /// </summary>
        public string DiskId; //硬盘的ID

        /// <summary>
        /// 硬盘大小
        /// </summary>
        public string DiskSize; //硬盘大小  单位：bytes

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress; //计算机的IP地址

        /// <summary>
        /// 系统当前登录用户
        /// </summary>
        public string LoginUserName; //操作系统登录用户名

        /// <summary>
        /// 计算机名
        /// </summary>
        public string ComputerName; //计算机名

        /// <summary>
        /// 操作系统架构
        /// </summary>
        public string SystemType; //系统类型

        /// <summary>
        /// 物理内存，单位MB
        /// </summary>
        public string TotalPhysicalMemory; //总共的内存  单位：M 

        private static WindowsServer _instance;

        /// <summary>
        /// 获取实例
        /// </summary>
        public static WindowsServer Instance => _instance ??= new WindowsServer();

        /// <summary>
        /// 构造函数
        /// </summary>
        public WindowsServer()
        {
            var cpuInfo = SystemInfo.GetCpuInfo();
            CpuId = cpuInfo[0].DeviceID;
            CpuCount = cpuInfo.Count;
            CpuMhz = cpuInfo.Select(c => c.CurrentClockSpeed).ToArray();
            MacAddress = SystemInfo.GetMacAddress()[0];
            DiskId = GetDiskID();
            DiskSize = GetSizeOfDisk();
            IpAddress = SystemInfo.GetLocalUsedIP().ToString();
            LoginUserName = GetUserName();
            SystemType = GetSystemType();
            TotalPhysicalMemory = GetTotalPhysicalMemory();
            ComputerName = GetComputerName();
        }

        /// <summary>
        /// 获取磁盘大小
        /// </summary>
        /// <returns></returns>
        public static string GetSizeOfDisk()
        {
            using var mc = new ManagementClass("Win32_DiskDrive");
            using var moc = mc.GetInstances();
            foreach (var m in moc)
            {
                using (m)
                {
                    return m.Properties["Size"].Value.ToString();
                }
            }

            return "-1";
        }

        string GetDiskID()
        {
            try
            {
                //获取硬盘ID 
                string hdid = Empty;
                using var mc = new ManagementClass("Win32_DiskDrive");
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    using (mo)
                    {
                        hdid = (string)mo.Properties["Model"].Value;
                    }
                }

                return hdid;
            }
            catch
            {
                return "unknow ";
            }
        }

        ///    <summary>  
        ///   操作系统的登录用户名 
        ///    </summary>  
        ///    <returns>  </returns>  
        string GetUserName()
        {
            try
            {
                string st = Empty;
                using var mc = new ManagementClass("Win32_ComputerSystem");
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    using (mo)
                    {
                        st = mo["UserName"].ToString();
                    }
                }

                return st;
            }
            catch
            {
                return "unknow ";
            }
        }

        string GetSystemType()
        {
            try
            {
                string st = Empty;
                using var mc = new ManagementClass("Win32_ComputerSystem");
                foreach (var o in mc.GetInstances())
                {
                    using (o)
                    {
                        var mo = (ManagementObject)o;
                        st = mo["SystemType"].ToString();
                    }
                }

                return st;
            }
            catch
            {
                return "unknow ";
            }
        }

        string GetTotalPhysicalMemory()
        {
            try
            {
                string st = Empty;
                using var mc = new ManagementClass("Win32_ComputerSystem");
                using var moc = mc.GetInstances();
                foreach (var o in moc)
                {
                    var mo = (ManagementObject)o;

                    st = mo["TotalPhysicalMemory"].ToString();
                }

                return st;
            }
            catch
            {
                return "unknow ";
            }
        }

        string GetComputerName()
        {
            try
            {
                return Environment.GetEnvironmentVariable("ComputerName");
            }
            catch
            {
                return "unknow ";
            }
        }
    }
}