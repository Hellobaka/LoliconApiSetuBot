using System.Collections.Generic;

namespace me.cqp.luohuaming.Setu.Code
{
    public class ErrorMsg
    {
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public string Date { get; set; }
    }
    public class SendError
    {
        public List<ErrorMsg> Msg { get; set; }
    }
}
