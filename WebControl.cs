using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace MajdataEdit
{
    static class WebControl
    {
        public static string RequestPOST(string url, string data = "")
        {
            try
            {
                WebClient wc = new WebClient();
                byte[] bufsend = Encoding.UTF8.GetBytes(data);
                byte[] buf = wc.UploadData(url, "POST", bufsend);
                string text = Encoding.UTF8.GetString(buf);
                return text;
            }
            catch { return "ERROR"; }
        }
    }
}
