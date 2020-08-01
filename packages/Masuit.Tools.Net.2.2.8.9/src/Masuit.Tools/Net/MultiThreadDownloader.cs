﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Masuit.Tools.Net
{
    /// <summary>
    /// 文件合并改变事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileMergeProgressChangedEventHandler(object sender, int e);

    /// <summary>
    /// 多线程下载器
    /// </summary>
    public class MultiThreadDownloader
    {
        #region 属性

        private string _url;
        private bool _rangeAllowed;
        private readonly HttpWebRequest _request;
        private Action<HttpWebRequest> _requestConfigure = req => req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.122 Safari/537.36";
        #endregion

        #region 公共属性

        /// <summary>
        /// RangeAllowed
        /// </summary>
        public bool RangeAllowed
        {
            get => _rangeAllowed;
            set => _rangeAllowed = value;
        }

        /// <summary>
        /// 临时文件夹
        /// </summary>
        public string TempFileDirectory { get; set; }

        /// <summary>
        /// url地址
        /// </summary>
        public string Url
        {
            get => _url;
            set => _url = value;
        }

        /// <summary>
        /// 第几部分
        /// </summary>
        public int NumberOfParts { get; set; }

        /// <summary>
        /// 已接收字节数
        /// </summary>
        public long TotalBytesReceived
        {
            get
            {
                try
                {
                    return PartialDownloaderList.Where(t => t != null).Sum(t => t.TotalBytesRead);
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// 总进度
        /// </summary>
        public float TotalProgress { get; private set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// 下载速度
        /// </summary>
        public float TotalSpeedInBytes => PartialDownloaderList.Sum(t => t.SpeedInBytes);

        /// <summary>
        /// 下载块
        /// </summary>
        public List<PartialDownloader> PartialDownloaderList { get; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        #endregion

        #region 变量

        /// <summary>
        /// 总下载进度更新事件
        /// </summary>
        public event EventHandler TotalProgressChanged;

        /// <summary>
        /// 文件合并事件
        /// </summary>
        public event FileMergeProgressChangedEventHandler FileMergeProgressChanged;

        private readonly AsyncOperation _aop;

        #endregion

        #region 下载管理器

        /// <summary>
        /// 多线程下载管理器
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="tempDir"></param>
        /// <param name="savePath"></param>
        /// <param name="numOfParts"></param>
        public MultiThreadDownloader(string sourceUrl, string tempDir, string savePath, int numOfParts)
        {
            _url = sourceUrl;
            NumberOfParts = numOfParts;
            TempFileDirectory = tempDir;
            PartialDownloaderList = new List<PartialDownloader>();
            _aop = AsyncOperationManager.CreateOperation(null);
            FilePath = savePath;
            _request = WebRequest.Create(sourceUrl) as HttpWebRequest;
        }

        /// <summary>
        /// 多线程下载管理器
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="savePath"></param>
        /// <param name="numOfParts"></param>
        public MultiThreadDownloader(string sourceUrl, string savePath, int numOfParts) : this(sourceUrl, null, savePath, numOfParts)
        {
            TempFileDirectory = Environment.GetEnvironmentVariable("temp");
        }

        /// <summary>
        /// 多线程下载管理器
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="numOfParts"></param>
        public MultiThreadDownloader(string sourceUrl, int numOfParts) : this(sourceUrl, null, numOfParts)
        {
        }

        #endregion

        #region 事件

        private void temp_DownloadPartCompleted(object sender, EventArgs e)
        {
            WaitOrResumeAll(PartialDownloaderList, true);

            if (TotalBytesReceived == Size)
            {
                UpdateProgress();
                MergeParts();
                return;
            }

            OrderByRemaining(PartialDownloaderList);
            int rem = PartialDownloaderList[0].RemainingBytes;
            if (rem < 50 * 1024)
            {
                WaitOrResumeAll(PartialDownloaderList, false);
                return;
            }

            int from = PartialDownloaderList[0].CurrentPosition + rem / 2;
            int to = PartialDownloaderList[0].To;
            if (from > to)
            {
                WaitOrResumeAll(PartialDownloaderList, false);
                return;
            }

            PartialDownloaderList[0].To = from - 1;
            WaitOrResumeAll(PartialDownloaderList, false);
            var temp = new PartialDownloader(_url, TempFileDirectory, Guid.NewGuid().ToString(), from, to, true);
            temp.DownloadPartCompleted += temp_DownloadPartCompleted;
            temp.DownloadPartProgressChanged += temp_DownloadPartProgressChanged;
            PartialDownloaderList.Add(temp);
            temp.Start(_requestConfigure);
        }

        void temp_DownloadPartProgressChanged(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        void UpdateProgress()
        {
            int pr = (int)(TotalBytesReceived * 1d / Size * 100);
            if (TotalProgress != pr)
            {
                TotalProgress = pr;
                if (TotalProgressChanged != null)
                {
                    _aop.Post(state => TotalProgressChanged(this, EventArgs.Empty), null);
                }
            }
        }

        #endregion

        #region 方法

        void CreateFirstPartitions()
        {
            Size = GetContentLength(ref _rangeAllowed, ref _url);
            int maximumPart = (int)(Size / (25 * 1024));
            maximumPart = maximumPart == 0 ? 1 : maximumPart;
            if (!_rangeAllowed)
            {
                NumberOfParts = 1;
            }
            else if (NumberOfParts > maximumPart)
            {
                NumberOfParts = maximumPart;
            }

            for (int i = 0; i < NumberOfParts; i++)
            {
                var temp = CreateNewPd(i, NumberOfParts, Size);
                temp.DownloadPartProgressChanged += temp_DownloadPartProgressChanged;
                temp.DownloadPartCompleted += temp_DownloadPartCompleted;
                PartialDownloaderList.Add(temp);
                temp.Start(_requestConfigure);
            }
        }

        void MergeParts()
        {
            var mergeOrderedList = SortPDsByFrom(PartialDownloaderList);
            var dir = new FileInfo(FilePath).DirectoryName;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite);
            long totalBytesWritten = 0;
            int mergeProgress = 0;
            foreach (var item in mergeOrderedList)
            {
                using var pds = new FileStream(item.FullPath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int read;
                while ((read = pds.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, read);
                    totalBytesWritten += read;
                    int temp = (int)(totalBytesWritten * 1d / Size * 100);
                    if (temp != mergeProgress && FileMergeProgressChanged != null)
                    {
                        mergeProgress = temp;
                        _aop.Post(state => FileMergeProgressChanged(this, temp), null);
                    }
                }

                try
                {
                    File.Delete(item.FullPath);
                }
                catch
                {
                    // ignored
                }
            }
        }

        PartialDownloader CreateNewPd(int order, int parts, long contentLength)
        {
            int division = (int)contentLength / parts;
            int remaining = (int)contentLength % parts;
            int start = division * order;
            int end = start + division - 1;
            end += (order == parts - 1) ? remaining : 0;
            return new PartialDownloader(_url, TempFileDirectory, Guid.NewGuid().ToString(), start, end, true);
        }

        /// <summary>
        /// 暂停或继续
        /// </summary>
        /// <param name="list"></param>
        /// <param name="wait"></param>
        public static void WaitOrResumeAll(List<PartialDownloader> list, bool wait)
        {
            foreach (var item in list)
            {
                if (wait)
                {
                    item.Wait();
                }
                else
                {
                    item.ResumeAfterWait();
                }
            }
        }

        /// <summary>
        /// 冒泡排序
        /// </summary>
        /// <param name="list"></param>
        private static void BubbleSort(List<PartialDownloader> list)
        {
            bool switched = true;
            while (switched)
            {
                switched = false;
                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (list[i].RemainingBytes < list[i + 1].RemainingBytes)
                    {
                        PartialDownloader temp = list[i];
                        list[i] = list[i + 1];
                        list[i + 1] = temp;
                        switched = true;
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the downloader by From property to merge the parts
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<PartialDownloader> SortPDsByFrom(List<PartialDownloader> list)
        {
            return list.OrderBy(x => x.From).ToList();
        }

        /// <summary>
        /// 按剩余时间排序
        /// </summary>
        /// <param name="list"></param>
        public static void OrderByRemaining(List<PartialDownloader> list)
        {
            BubbleSort(list);
        }

        /// <summary>
        /// 配置请求头
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Action<HttpWebRequest> config)
        {
            _requestConfigure = config;
        }

        /// <summary>
        /// 获取内容长度
        /// </summary>
        /// <param name="url"></param>
        /// <param name="rangeAllowed"></param>
        /// <param name="redirectedUrl"></param>
        /// <returns></returns>
        public long GetContentLength(ref bool rangeAllowed, ref string redirectedUrl)
        {
            _request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.122 Safari/537.36";
            _request.ServicePoint.ConnectionLimit = 4;
            _requestConfigure(_request);
            using var resp = _request.GetResponse() as HttpWebResponse;
            redirectedUrl = resp.ResponseUri.OriginalString;
            var ctl = resp.ContentLength;
            rangeAllowed = resp.Headers.AllKeys.Select((v, i) => new
            {
                HeaderName = v,
                HeaderValue = resp.Headers[i]
            }).Any(k => k.HeaderName.ToLower().Contains("range") && k.HeaderValue.ToLower().Contains("byte"));
            _request.Abort();
            return ctl;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 暂停下载
        /// </summary>
        public void Pause()
        {
            foreach (var t in PartialDownloaderList.Where(t => !t.Completed))
            {
                t.Stop();
            }

            Thread.Sleep(200);
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        public void Start()
        {
            Task th = new Task(CreateFirstPartitions);
            th.Start();
        }

        /// <summary>
        /// 唤醒下载
        /// </summary>
        public void Resume()
        {
            int count = PartialDownloaderList.Count;
            for (int i = 0; i < count; i++)
            {
                if (PartialDownloaderList[i].Stopped)
                {
                    int from = PartialDownloaderList[i].CurrentPosition + 1;
                    int to = PartialDownloaderList[i].To;
                    if (from > to)
                    {
                        continue;
                    }

                    var temp = new PartialDownloader(_url, TempFileDirectory, Guid.NewGuid().ToString(), from, to, _rangeAllowed);
                    temp.DownloadPartProgressChanged += temp_DownloadPartProgressChanged;
                    temp.DownloadPartCompleted += temp_DownloadPartCompleted;
                    PartialDownloaderList.Add(temp);
                    PartialDownloaderList[i].To = PartialDownloaderList[i].CurrentPosition;
                    temp.Start(_requestConfigure);
                }
            }
        }

        #endregion
    }
}