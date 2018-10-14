# region

#endregion

namespace PecanDB.Remoting
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.SelfHost;

    public class RemoteAccess
    {
        internal static void RunServer(string endpoint)
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        var config = new HttpSelfHostConfiguration(endpoint);
                        config.Routes.MapHttpRoute(
                            "API Default",
                            "api/{controller}/{action}");
                        var server = new HttpSelfHostServer(config);
                        await server.OpenAsync();
                        await Task.Delay(TimeSpan.MaxValue);
                        server.Dispose();
                    }
                });
        }

        internal static T MakeRequest<T>(string endpoint, string action)
        {
            ServicePointManager.MaxServicePointIdleTime = Timeout.Infinite;

            var requestHandler = new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };

            var _client = new HttpClient(requestHandler);
            _client.Timeout = TimeSpan.FromMinutes(10);
            _client.BaseAddress = new Uri(endpoint);

            var result = _client.GetAsync("api/pecan/" + action).Result;
            result.EnsureSuccessStatusCode();
            var products = result.Content.ReadAsAsync<T>().Result;
            return products;
        }
    }
}