using Scighost.PixivApi.Illust;
using System.Collections.Generic;

namespace me.cqp.luohuaming.Setu.PublicInfos.API
{
    public static class PixivAPI
    {
        public static IllustInfo GetPicInfo(int id)
        {
            var task = MainSave.PixivClient.GetIllustInfoAsync(id);
            IllustInfo info = task.Result;
            task.Wait();
            return info;
        }

        public static string DownloadPic(int id, string path)
        {
            IllustInfo info = GetPicInfo(id);
            var downloadTask = MainSave.PixivClient.DownloadPic(info, path);
            string filename = downloadTask.Result;
            downloadTask.Wait();
            return filename;
        }

        public static string DownloadPic(string url, string path)
        {
            var downloadTask = MainSave.PixivClient.DownloadPic(url, path);
            string filename = downloadTask.Result;
            downloadTask.Wait();
            return filename;
        }

        public static List<IllustRankItem> GetRankList(RankType type)
        {
            var task = MainSave.PixivClient.GetRankingList(type);
            var result = task.Result;
            task.Wait();
            return result;
        }
    }
}
