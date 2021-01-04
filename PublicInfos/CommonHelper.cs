using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.Model;
using Native.Tool.IniConfig;

namespace PublicInfos
{
    public static class CommonHelper
    {
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        public static bool CheckAdmin(long QQID)
        {
            IniConfig ini = MainSave.ConfigMain;
            int count = ini.Object["Admin"]["Count"].GetValueOrDefault(0);
            for (int i = 0; i < count; i++)
            {
                if (ini.Object["Admin"][$"Index{i}"].GetValueOrDefault((long)0) == QQID)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool CheckCustomObject(List<CustomObject> ls, string orderText)
        {
            if (ls.Count == 0 || string.IsNullOrWhiteSpace(orderText)) return false;
            foreach (var item in ls)
            {
                if (orderText == item.Order && item.Enabled)
                {
                    return true;
                }
            }
            return false;
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
        /// <summary>
        /// 改变图片的MD5来尝试反和谐
        /// </summary>
        /// <param name="img">原图</param>
        /// <returns></returns>
        public static void AntiHX(string path)
        {
            Image img = Image.FromFile(path);
            Bitmap bitMap = new Bitmap(img.Width, img.Height);
            Graphics g1 = Graphics.FromImage(bitMap);

            Color pixelColor = bitMap.GetPixel(0, 0);
            Color targetcolor = ChangeColor(pixelColor);
            Pen pen = new Pen(targetcolor);
            Rectangle rect = new Rectangle(0, 0, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(img.Width - 1, 0);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(img.Width - 1, 0, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(0, img.Height - 1);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(0, img.Height - 1, 1, 1);
            g1.DrawRectangle(pen, rect);

            pixelColor = bitMap.GetPixel(img.Width - 1, img.Height - 1);
            targetcolor = ChangeColor(pixelColor);
            pen = new Pen(targetcolor);
            rect = new Rectangle(img.Width - 1, img.Height - 1, 1, 1);
            g1.DrawRectangle(pen, rect);

            g1.Dispose();
            img.Save(path + ".tmp");
            img.Dispose();
            File.Delete(path);
            File.Move(path + ".tmp", path);
        }
        /// <summary>
        /// 颜色轻微变更
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        private static Color ChangeColor(Color col)
        {
            byte red, blue, green;
            if (col.R == 0 || col.R == 255)
            {
                red = (col.R == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                red = Convert.ToByte(col.R + 1);
            }
            if (col.G == 0 || col.G == 255)
            {
                green = (col.G == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                green = Convert.ToByte(col.G + 1);
            }
            if (col.B == 0 || col.B == 255)
            {
                blue = (col.B == 0) ? (byte)1 : Convert.ToByte(244);
            }
            else
            {
                blue = Convert.ToByte(col.R + 1);
            }
            Color result = Color.FromArgb(red, green, blue);
            return result;
        }
    }
}
