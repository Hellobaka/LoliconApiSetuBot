using me.cqp.luohuaming.Setu.Code.Deserializtion.HotSearch;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Pixiv_HotSearch hotSearch = JsonConvert.DeserializeObject<Pixiv_HotSearch>(File.ReadAllText(@"E:\test.txt"));
            var info = hotSearch.data.Where(x => x.tags.Any(y => y.name.Contains("114514")))
                .OrderBy(x => Guid.NewGuid().ToString()).FirstOrDefault();
            Console.ReadKey();
        }
    }
}
