﻿using Masuit.Tools.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Masuit.Tools.Hardware
{
    /// <summary>
    /// 硬件信息，部分功能需要C++支持
    /// </summary>
    public static partial class SystemInfo
    {
        #region 字段

        private const int GwHwndfirst = 0;
        private const int GwHwndnext = 2;
        private const int GwlStyle = -16;
        private const int WsVisible = 268435456;
        private const int WsBorder = 8388608;
        private static readonly PerformanceCounter PcCpuLoad; //CPU计数器 

        private static readonly PerformanceCounter MemoryCounter = new PerformanceCounter();
        private static readonly PerformanceCounter CpuCounter = new PerformanceCounter();
        private static readonly PerformanceCounter DiskReadCounter = new PerformanceCounter();
        private static readonly PerformanceCounter DiskWriteCounter = new PerformanceCounter();

        private static readonly string[] InstanceNames;
        private static readonly PerformanceCounter[] NetRecvCounters;
        private static readonly PerformanceCounter[] NetSentCounters;

        #endregion

        #region 构造函数 

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static SystemInfo()
        {
            //初始化CPU计数器 
            PcCpuLoad = new PerformanceCounter("Processor", "% Processor Time", "_Total")
            {
                MachineName = "."
            };
            PcCpuLoad.NextValue();

            //CPU个数 
            ProcessorCount = Environment.ProcessorCount;

            //获得物理内存 
            try
            {
                using var mc = new ManagementClass("Win32_ComputerSystem");
                using var moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    if (mo["TotalPhysicalMemory"] != null)
                    {
                        PhysicalMemory = long.Parse(mo["TotalPhysicalMemory"].ToString());
                    }
                }

                var cat = new PerformanceCounterCategory("Network Interface");
                InstanceNames = cat.GetInstanceNames();
                NetRecvCounters = new PerformanceCounter[InstanceNames.Length];
                NetSentCounters = new PerformanceCounter[InstanceNames.Length];
                for (int i = 0; i < InstanceNames.Length; i++)
                {
                    NetRecvCounters[i] = new PerformanceCounter();
                    NetSentCounters[i] = new PerformanceCounter();
                }

                CompactFormat = false;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
            }
        }

        #endregion

        private static bool CompactFormat { get; set; }

        #region CPU核心 

        /// <summary>
        /// 获取CPU核心数 
        /// </summary>
        public static int ProcessorCount { get; }

        #endregion

        #region CPU占用率 

        /// <summary>
        /// 获取CPU占用率 %
        /// </summary>
        public static float CpuLoad => PcCpuLoad.NextValue();

        #endregion

        #region 可用内存 

        /// <summary>
        /// 获取可用内存
        /// </summary>
        public static long MemoryAvailable
        {
            get
            {
                try
                {
                    using var mos = new ManagementClass("Win32_OperatingSystem");
                    foreach (var mo in mos.GetInstances())
                    {
                        if (mo["FreePhysicalMemory"] != null)
                        {
                            return 1024 * long.Parse(mo["FreePhysicalMemory"].ToString());
                        }
                    }

                    return 0;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        #endregion

        #region 物理内存 

        /// <summary>
        /// 获取物理内存
        /// </summary>
        public static long PhysicalMemory { get; }

        #endregion

        #region 查找所有应用程序标题 

        /// <summary>
        /// 查找所有应用程序标题 
        /// </summary>
        /// <param name="handle">应用程序标题范型</param>
        /// <returns>所有应用程序集合</returns>
        public static ArrayList FindAllApps(int handle)
        {
            var apps = new ArrayList();
            int hwCurr = GetWindow(handle, GwHwndfirst);

            while (hwCurr > 0)
            {
                int IsTask = WsVisible | WsBorder;
                int lngStyle = GetWindowLongA(hwCurr, GwlStyle);
                bool taskWindow = (lngStyle & IsTask) == IsTask;
                if (taskWindow)
                {
                    int length = GetWindowTextLength(new IntPtr(hwCurr));
                    StringBuilder sb = new StringBuilder(2 * length + 1);
                    GetWindowText(hwCurr, sb, sb.Capacity);
                    string strTitle = sb.ToString();
                    if (!string.IsNullOrEmpty(strTitle))
                    {
                        apps.Add(strTitle);
                    }
                }

                hwCurr = GetWindow(hwCurr, GwHwndnext);
            }

            return apps;
        }

        #endregion

        #region 获取CPU的数量

        /// <summary>
        /// 获取CPU的数量
        /// </summary>
        /// <returns>CPU的数量</returns>
        public static int GetCpuCount()
        {
            try
            {
                using var m = new ManagementClass("Win32_Processor");
                using var mn = m.GetInstances();
                return mn.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region 获取CPU信息

        private static List<ManagementBaseObject> _cpuObjects;

        /// <summary>
        /// 获取CPU信息
        /// </summary>
        /// <returns>CPU信息</returns>
        public static List<CpuInfo> GetCpuInfo()
        {
            try
            {
                using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                using var moc = mos.Get();
                _cpuObjects ??= moc.AsParallel().Cast<ManagementBaseObject>().ToList();
                return _cpuObjects.Select(mo => new CpuInfo
                {
                    CpuLoad = CpuLoad,
                    NumberOfLogicalProcessors = ProcessorCount,
                    CurrentClockSpeed = mo.Properties["CurrentClockSpeed"].Value.ToString(),
                    Manufacturer = mo.Properties["Manufacturer"].Value.ToString(),
                    MaxClockSpeed = mo.Properties["MaxClockSpeed"].Value.ToString(),
                    Type = mo.Properties["Name"].Value.ToString(),
                    DataWidth = mo.Properties["DataWidth"].Value.ToString(),
                    DeviceID = mo.Properties["DeviceID"].Value.ToString(),
                    NumberOfCores = Convert.ToInt32(mo.Properties["NumberOfCores"].Value),
                    Temperature = GetCPUTemperature()
                }).ToList();
            }
            catch (Exception)
            {
                return new List<CpuInfo>();
            }
        }

        #endregion

        #region 获取内存信息

        /// <summary>
        /// 获取内存信息
        /// </summary>
        /// <returns>内存信息</returns>
        public static RamInfo GetRamInfo()
        {
            var info = new RamInfo
            {
                MemoryAvailable = GetFreePhysicalMemory(),
                PhysicalMemory = GetTotalPhysicalMemory(),
                TotalPageFile = GetTotalVirtualMemory(),
                AvailablePageFile = GetTotalVirtualMemory() - GetUsedVirtualMemory(),
                AvailableVirtual = 1 - GetUsageVirtualMemory(),
                TotalVirtual = 1 - GetUsedPhysicalMemory()
            };
            return info;
        }

        #endregion

        #region 获取CPU温度

        /// <summary>
        /// 获取CPU温度
        /// </summary>
        /// <returns>CPU温度</returns>
        public static double GetCPUTemperature()
        {
            try
            {
                string str = "";
                using var mos = new ManagementObjectSearcher(@"root\WMI", "select * from MSAcpi_ThermalZoneTemperature");
                var moc = mos.Get();
                foreach (var mo in moc)
                {
                    str += mo.Properties["CurrentTemperature"].Value.ToString();
                }

                //这就是CPU的温度了
                double temp = (double.Parse(str) - 2732) / 10;
                return Math.Round(temp, 2);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region WMI接口获取CPU使用率

        /// <summary>
        /// WMI接口获取CPU使用率
        /// </summary>
        /// <returns></returns>
        public static string GetProcessorData()
        {
            double d = GetCounterValue(CpuCounter, "Processor", "% Processor Time", "_Total");
            return CompactFormat ? (int)d + "%" : d.ToString("F") + "%";
        }

        #endregion

        #region 获取虚拟内存使用率详情

        /// <summary>
        /// 获取虚拟内存使用率详情
        /// </summary>
        /// <returns></returns>
        public static string GetMemoryVData()
        {
            double d = GetCounterValue(MemoryCounter, "Memory", "% Committed Bytes In Use", null);
            var str = d.ToString("F") + "% (";

            d = GetCounterValue(MemoryCounter, "Memory", "Committed Bytes", null);
            str += FormatBytes(d) + " / ";

            d = GetCounterValue(MemoryCounter, "Memory", "Commit Limit", null);
            return str + FormatBytes(d) + ") ";
        }

        /// <summary>
        /// 获取虚拟内存使用率
        /// </summary>
        /// <returns></returns>
        public static double GetUsageVirtualMemory()
        {
            return GetCounterValue(MemoryCounter, "Memory", "% Committed Bytes In Use", null);
        }

        /// <summary>
        /// 获取虚拟内存已用大小
        /// </summary>
        /// <returns></returns>
        public static double GetUsedVirtualMemory()
        {
            return GetCounterValue(MemoryCounter, "Memory", "Committed Bytes", null);
        }

        /// <summary>
        /// 获取虚拟内存总大小
        /// </summary>
        /// <returns></returns>
        public static double GetTotalVirtualMemory()
        {
            return GetCounterValue(MemoryCounter, "Memory", "Commit Limit", null);
        }

        #endregion

        #region 获取物理内存使用率详情

        /// <summary>
        /// 获取物理内存使用率详情描述
        /// </summary>
        /// <returns></returns>
        public static string GetMemoryPData()
        {
            string s = QueryComputerSystem("totalphysicalmemory");
            double totalphysicalmemory = Convert.ToDouble(s);

            double d = GetCounterValue(MemoryCounter, "Memory", "Available Bytes", null);
            d = totalphysicalmemory - d;

            s = CompactFormat ? "%" : "% (" + FormatBytes(d) + " / " + FormatBytes(totalphysicalmemory) + ")";
            d /= totalphysicalmemory;
            d *= 100;
            return CompactFormat ? (int)d + s : d.ToString("F") + s;
        }

        /// <summary>
        /// 获取物理内存总数，单位B
        /// </summary>
        /// <returns></returns>
        public static double GetTotalPhysicalMemory()
        {
            string s = QueryComputerSystem("totalphysicalmemory");
            return s.ToDouble();
        }

        /// <summary>
        /// 获取空闲的物理内存数，单位B
        /// </summary>
        /// <returns></returns>
        public static double GetFreePhysicalMemory()
        {
            return GetCounterValue(MemoryCounter, "Memory", "Available Bytes", null);
        }

        /// <summary>
        /// 获取已经使用了的物理内存数，单位B
        /// </summary>
        /// <returns></returns>
        public static double GetUsedPhysicalMemory()
        {
            return GetTotalPhysicalMemory() - GetFreePhysicalMemory();
        }

        #endregion

        #region 获取硬盘的读写速率

        /// <summary>
        /// 获取硬盘的读写速率
        /// </summary>
        /// <param name="dd">读或写</param>
        /// <returns></returns>
        public static double GetDiskData(DiskData dd) => dd == DiskData.Read ? GetCounterValue(DiskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") : dd == DiskData.Write ? GetCounterValue(DiskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") : dd == DiskData.ReadAndWrite ? GetCounterValue(DiskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") + GetCounterValue(DiskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") : 0;

        #endregion

        #region 获取网络的传输速率

        /// <summary>
        /// 获取网络的传输速率
        /// </summary>
        /// <param name="nd">上传或下载</param>
        /// <returns></returns>
        public static double GetNetData(NetData nd)
        {
            if (InstanceNames.Length == 0)
            {
                return 0;
            }

            double d = 0;
            for (int i = 0; i < InstanceNames.Length; i++)
            {
                double receied = GetCounterValue(NetRecvCounters[i], "Network Interface", "Bytes Received/sec", InstanceNames[i]);
                double send = GetCounterValue(NetSentCounters[i], "Network Interface", "Bytes Sent/sec", InstanceNames[i]);
                switch (nd)
                {
                    case NetData.Received:
                        d += receied;
                        break;
                    case NetData.Sent:
                        d += send;
                        break;
                    case NetData.ReceivedAndSent:
                        d += receied + send;
                        break;
                    default:
                        d += 0;
                        break;
                }
            }

            return d;
        }

        #endregion

        /// <summary>
        /// 获取网卡硬件地址
        /// </summary>
        /// <returns></returns>
        public static IList<string> GetMacAddress()
        {
            //获取网卡硬件地址       
            try
            {
                IList<string> list = new List<string>();
                using var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                using var moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    if ((bool)mo["IPEnabled"])
                    {
                        list.Add(mo["MacAddress"].ToString());
                    }
                }

                return list;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>  
        /// 获取当前使用的IP  
        /// </summary>  
        /// <returns></returns>  
        public static IPAddress GetLocalUsedIP()
        {
            return NetworkInterface.GetAllNetworkInterfaces().OrderByDescending(c => c.Speed).Where(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(u => u.Address)).FirstOrDefault();
        }

        /// <summary>  
        /// 获取本机所有的ip地址
        /// </summary>  
        /// <returns></returns>  
        public static List<UnicastIPAddressInformation> GetLocalIPs()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().OrderByDescending(c => c.Speed).Where(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up); //所有网卡信息
            return interfaces.SelectMany(n => n.GetIPProperties().UnicastAddresses).ToList();
        }

        #region 将速度值格式化成字节单位

        /// <summary>
        /// 将速度值格式化成字节单位
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string FormatBytes(this double bytes)
        {
            int unit = 0;
            while (bytes > 1024)
            {
                bytes /= 1024;
                ++unit;
            }

            string s = CompactFormat ? ((int)bytes).ToString() : bytes.ToString("F") + " ";
            return s + (Unit)unit;
        }

        #endregion

        #region 查询计算机系统信息

        /// <summary>
        /// 获取计算机开机时间
        /// </summary>
        /// <returns>datetime</returns>
        public static DateTime BootTime()
        {
            var query = new SelectQuery("SELECT LastBootUpTime FROM Win32_OperatingSystem WHERE Primary='true'");
            using var searcher = new ManagementObjectSearcher(query);
            foreach (var mo in searcher.Get())
            {
                return ManagementDateTimeConverter.ToDateTime(mo.Properties["LastBootUpTime"].Value.ToString());
            }

            return DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount & int.MaxValue);
        }

        /// <summary>
        /// 查询计算机系统信息
        /// </summary>
        /// <param name="type">类型名</param>
        /// <returns></returns>
        public static string QueryComputerSystem(string type)
        {
            try
            {
                string str = null;
                var mos = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (var mo in mos.Get())
                {
                    str = mo[type].ToString();
                }
                return str;
            }
            catch (Exception e)
            {
                return "未能获取到当前计算机系统信息，可能是当前程序无管理员权限，如果是web应用程序，请将应用程序池的高级设置中的进程模型下的标识设置为：LocalSystem；如果是普通桌面应用程序，请提升管理员权限后再操作。异常信息：" + e.Message;
            }
        }

        #endregion

        #region 获取环境变量

        /// <summary>
        /// 获取环境变量
        /// </summary>
        /// <param name="type">环境变量名</param>
        /// <returns></returns>
        public static string QueryEnvironment(string type) => Environment.ExpandEnvironmentVariables(type);

        #endregion

        #region 获取磁盘空间

        /// <summary>
        /// 获取磁盘可用空间
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> DiskFree()
        {
            try
            {
                var dic = new Dictionary<string, string>();
                var mos = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
                foreach (var mo in mos.Get())
                {
                    if (null != mo["DeviceID"] && null != mo["FreeSpace"])
                    {
                        dic.Add(mo["DeviceID"].ToString(), FormatBytes(double.Parse(mo["FreeSpace"].ToString())));
                    }
                }

                return dic;
            }
            catch (Exception)
            {
                return new Dictionary<string, string>()
                {
                    { "null", "未能获取到当前计算机的磁盘信息，可能是当前程序无管理员权限，如果是web应用程序，请将应用程序池的高级设置中的进程模型下的标识设置为：LocalSystem；如果是普通桌面应用程序，请提升管理员权限后再操作。" }
                };
            }
        }

        /// <summary>
        /// 获取磁盘总空间
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> DiskTotalSpace()
        {
            try
            {
                var dic = new Dictionary<string, string>();
                using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
                foreach (var mo in mos.Get())
                {
                    if (null != mo["DeviceID"] && null != mo["Size"])
                    {
                        dic.Add(mo["DeviceID"].ToString(), FormatBytes(double.Parse(mo["Size"].ToString())));
                    }
                }

                return dic;
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }


        /// <summary>
        /// 获取磁盘已用空间
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> DiskUsedSpace()
        {
            try
            {
                var dic = new Dictionary<string, string>();
                using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
                foreach (var mo in mos.Get())
                {
                    if (null != mo["DeviceID"] && null != mo["Size"])
                    {
                        var free = mo["FreeSpace"];
                        dic.Add(mo["DeviceID"].ToString(), FormatBytes(double.Parse(mo["Size"].ToString()) - free.ToString().ToDouble()));
                    }
                }

                return dic;
            }
            catch (Exception)
            {
                return new Dictionary<string, string>()
                {
                    { "null", "未能获取到当前计算机的磁盘信息，可能是当前程序无管理员权限，如果是web应用程序，请将应用程序池的高级设置中的进程模型下的标识设置为：LocalSystem；如果是普通桌面应用程序，请提升管理员权限后再操作。" }
                };
            }
        }

        /// <summary>
        /// 获取磁盘使用率
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, double> DiskUsage()
        {
            try
            {
                var dic = new Dictionary<string, double>();
                using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
                foreach (var mo in mos.Get())
                {
                    var device = mo["DeviceID"];
                    if (null != device)
                    {
                        var free = mo["FreeSpace"];
                        var total = mo["Size"];
                        if (null != total && total.ToString().ToDouble() > 0)
                        {
                            dic.Add(device.ToString(), 1 - free.ToString().ToDouble() / total.ToString().ToDouble());
                        }
                    }
                }

                return dic;
            }
            catch (Exception)
            {
                return new Dictionary<string, double>()
                {
                    { "未能获取到当前计算机的磁盘信息，可能是当前程序无管理员权限，如果是web应用程序，请将应用程序池的高级设置中的进程模型下的标识设置为：LocalSystem；如果是普通桌面应用程序，请提升管理员权限后再操作。", 0 }
                };
            }
        }

        #endregion

        private static double GetCounterValue(PerformanceCounter pc, string categoryName, string counterName, string instanceName)
        {
            pc.CategoryName = categoryName;
            pc.CounterName = counterName;
            pc.InstanceName = instanceName;
            return pc.NextValue();
        }

        #region Win32API声明 

#pragma warning disable 1591
        [DllImport("kernel32")]
        public static extern void GetWindowsDirectory(StringBuilder winDir, int count);

        [DllImport("kernel32")]
        public static extern void GetSystemDirectory(StringBuilder sysDir, int count);

        [DllImport("kernel32")]
        public static extern void GetSystemInfo(ref CPU_INFO cpuinfo);

        [DllImport("kernel32")]
        public static extern void GlobalMemoryStatus(ref MemoryInfo meminfo);

        [DllImport("kernel32")]
        public static extern void GetSystemTime(ref SystemtimeInfo stinfo);

        [DllImport("IpHlpApi.dll")]
        public static extern uint GetIfTable(byte[] pIfTable, ref uint pdwSize, bool bOrder);

        [DllImport("User32")]
        public static extern int GetWindow(int hWnd, int wCmd);

        [DllImport("User32")]
        public static extern int GetWindowLongA(int hWnd, int wIndx);

        [DllImport("user32.dll")]
        public static extern bool GetWindowText(int hWnd, StringBuilder title, int maxBufSize);

        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
#pragma warning restore 1591

        #endregion
    }
}