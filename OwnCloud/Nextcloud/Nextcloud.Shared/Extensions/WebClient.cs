using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Nextcloud.Converter
{
    class WebClient
    {
        protected Uri address;
        protected NetworkCredential credentials;
        protected bool busy;
        protected HttpWebRequest request;

        public WebClient(Uri address, NetworkCredential credentials=null) {
            this.address = address;
            this.credentials = credentials;
        }
        public WebClient(string address, NetworkCredential credentials=null) {
            this.address = new Uri(address);
            this.credentials = credentials;
        }
        public WebClient(Uri address, string username, string password) {
            this.address = address;
            this.credentials = new NetworkCredential(username, password);
        }
        public WebClient(string address, string username, string password) {
            this.address = new Uri(address);
            this.credentials = new NetworkCredential(username, password);
        }

        public void Connect() {

        }
    }
}
