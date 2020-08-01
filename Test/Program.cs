using Masuit.Tools.Net;
using me.cqp.luohuaming.Setu.Code;
using me.cqp.luohuaming.Setu.Code.Deserializtion;
using Native.Tool.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program { 
        static void Main(string[] args)
        {
            string url = "https://saucenao.com/search.php?output_type=2&api_key=56faa0cddf50860330a295e0c331be7c4b4c021f&db=999&numres=1&url=";
            url += "https://gchat.qpic.cn/gchatpic_new/863450594/891787846-2734995023-9C30D4D29E2ABAEEAB78F58ED58BDD84/0?term=2";
            Console.WriteLine(url);
            string str = new HttpWebClient().DownloadString(url);
            Console.WriteLine(str);
            var result = JsonConvert.DeserializeObject<SauceNao_Deserial.SauceNAO>(str);
            Console.ReadKey();
        }
    }
}
