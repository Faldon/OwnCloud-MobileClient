using System;
using System.Collections.Generic;
using System.Text;
using Windows.Web.Http;

namespace Nextcloud.Extensions
{
    class WebDAVClient
    {
        private Uri _server;
        private HttpClient _client;

        public WebDAVClient(string webDAVPath) {
            _server = new Uri(webDAVPath);
            _client = new HttpClient();
        }

        public WebDAVClient(Uri webDAVUri) {
            _server = webDAVUri;
            _client = new HttpClient();
        }

        private void Connect() {
            HttpRequestMessage connectTest = new HttpRequestMessage(HttpMethod.Options, _server);
        }
    }
}
