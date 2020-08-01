using Masuit.Tools.Media;
using Native.Sdk.Cqp;
using System;
using System.IO;

namespace me.cqp.luohuaming.Setu.Code
{
    public class CompressImg
    {        
        public static string CompressImage(string picpath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(CQSave.ImageDirectory+picpath);
                string newFilePath = fileInfo.FullName.Replace(fileInfo.Extension, "_Compress" + fileInfo.Extension);
                ImageUtilities.CompressImage(CQSave.ImageDirectory + picpath, newFilePath);
                CQSave.cqlog.Info("图片压缩", $"压缩成功，压缩后图片已保存至"+newFilePath);
                return CQApi.CQCode_Image(picpath.Replace(fileInfo.Extension, "_Compress"+fileInfo.Extension)).ToSendString();
            }
            catch(Exception e)
            {
                CQSave.cqlog.Info("图片压缩", $"压缩失败，错误信息:{e.Message}");
                return picpath;
            }
        }
    }
}
