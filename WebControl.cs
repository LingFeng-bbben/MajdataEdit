using System.Net;
using System.Text;

namespace MajdataEdit;

internal static class WebControl
{
    public static string RequestPOST(string url, string data = "")
    {
        try
        {
            var wc = new WebClient();
            var bufsend = Encoding.UTF8.GetBytes(data);
            var buf = wc.UploadData(url, "POST", bufsend);
            var text = Encoding.UTF8.GetString(buf);
            return text;
        }
        catch
        {
            return "ERROR";
        }
    }

    public static void RequestGETAsync(string url, DownloadDataCompletedEventHandler handler)
    {
        var wc = new WebClient();
        wc.Headers.Clear();
        wc.Headers.Add("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36");
        wc.DownloadDataCompleted += handler;
        wc.DownloadDataAsync(new Uri(url));
    }
}