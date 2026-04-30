using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Models;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class PatreonRemoteFilenameRetrieverTests
    {
        [Fact]
        public async Task RetrieveRemoteFileName_SendsConfiguredCookieUserAgentAndReferer()
        {
            using TestHttpServer server = new TestHttpServer(
                "HTTP/1.1 200 OK\r\nContent-Disposition: attachment; filename=\"protected.zip\"\r\nContent-Length: 0\r\n\r\n");
            PatreonRemoteFilenameRetriever retriever = new PatreonRemoteFilenameRetriever();
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.Add(server.BaseUri, new Cookie("session_id", "abc123"));

            await retriever.BeforeStart(new PatreonDownloaderSettings
            {
                CookieContainer = cookieContainer,
                UserAgent = "UnitTestAgent/1.0"
            });

            string filename = await retriever.RetrieveRemoteFileName(server.Url);

            Assert.Equal("protected.zip", filename);
            Assert.Contains("Cookie: session_id=abc123", server.RequestHeaders);
            Assert.Contains("User-Agent: UnitTestAgent/1.0", server.RequestHeaders);
            Assert.Contains("Referer: https://www.patreon.com/", server.RequestHeaders);
        }

        [Fact]
        public async Task RetrieveRemoteFileName_PatreonMediaUrlWithoutName_FallsBackToContentType()
        {
            using TestHttpServer server = new TestHttpServer(
                "HTTP/1.1 200 OK\r\nContent-Type: application/zip\r\nContent-Length: 0\r\n\r\n");
            PatreonRemoteFilenameRetriever retriever = new PatreonRemoteFilenameRetriever();

            await retriever.BeforeStart(new PatreonDownloaderSettings());

            string filename = await retriever.RetrieveRemoteFileName(server.Url);

            Assert.StartsWith("gen_", filename);
            Assert.EndsWith(".zip", filename);
        }

        private sealed class TestHttpServer : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly Task _serverTask;
            private readonly string _response;

            public Uri BaseUri { get; }
            public string Url => BaseUri.ToString();
            public string RequestHeaders { get; private set; }

            public TestHttpServer(string response)
            {
                _response = response;
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                BaseUri = new Uri($"http://127.0.0.1:{port}/media-u/v3/303605900");
                _serverTask = Task.Run(HandleRequest);
            }

            private async Task HandleRequest()
            {
                using TcpClient client = await _listener.AcceptTcpClientAsync();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[8192];
                int length = await stream.ReadAsync(buffer, 0, buffer.Length);
                RequestHeaders = Encoding.ASCII.GetString(buffer, 0, length);
                byte[] responseBytes = Encoding.ASCII.GetBytes(_response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }

            public void Dispose()
            {
                _listener.Stop();
                _serverTask.Wait(TimeSpan.FromSeconds(5));
            }
        }
    }
}
