﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.Model;
using Native.Tool.IniConfig;

namespace me.cqp.luohuaming.Setu.PublicInfos
{
    public static class CommonHelper
    {
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// 获取CQ码中的图片网址
        /// </summary>
        /// <param name="imageCQCode">需要解析的图片CQ码</param>
        /// <returns></returns>
        public static string GetImageURL(string imageCQCode)
        {
            string path = MainSave.ImageDirectory + CQCode.Parse(imageCQCode)[0].Items["file"] + ".cqimg";
            IniConfig image = new IniConfig(path);
            image.Load();
            return image.Object["image"]["url"].ToString();
        }

        public static string GetAppImageDirectory()
        {
            var ImageDirectory = Path.Combine(Environment.CurrentDirectory, "data", "image\\");
            return ImageDirectory;
        }

        public static int ToInt(this string str) => int.TryParse(str, out int value) ? value : -1;

        public static string Join<T>(this IEnumerable<T> ls, string pattern)
        {
            string str = "";
            foreach (var item in ls)
                str += item.ToString() + pattern;
            if(str.Length > 0) str = str.Substring(0, str.Length - pattern.Length);
            return str;
        }
    }
}
