using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nextcloud.Tests {
    class WebDAVTest {
        public WebDAVTest() {
            WebDAV client = new WebDAV(new Uri("https://cloudspace.thesecretgamer.de/remote.php/webdav/"), new NetworkCredential("faldon", "MyS3cr3tP455w0rd"));
            client.StartRequest(DAVRequestHeader.CreateListing("/"), DAVRequestBody.CreateAllPropertiesListing(), null, RequestCompleted);
        }
        
        private void RequestCompleted(DAVRequestResult result, object userObj) {
            System.Diagnostics.Debug.WriteLine(result.Status);
            System.Diagnostics.Debug.WriteLine(result.Items);
        }
        
    }
}
