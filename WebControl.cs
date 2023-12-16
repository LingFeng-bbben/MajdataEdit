using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace MajdataEdit;

internal static class WebControl
{
    public static string RequestPOST(string url, string data = "")
    {
        try
        {
            using var client = new HttpClient();

            var webRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(data, Encoding.UTF8)
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            return reader.ReadToEnd();
        }
        catch
        {
            return "ERROR";
        }
    }

    public static string RequestGETAsync(string url)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", $"{executingAssembly.GetName().Name!} / {executingAssembly.GetName().Version!.ToString(3)}");
        var response = httpClient.Send(request);
        using var reader = new StreamReader(response.Content.ReadAsStream());

        return reader.ReadToEnd();
    }
}