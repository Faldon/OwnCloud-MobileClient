using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Servers")]
    class Server : IEntity
    {
        [PrimaryKey]
        public string FQDN { get; set; }

        [NotNull]
        public string Protocol { get; set; }

        [NotNull]
        public string WebDAVPath { get; set; }

        [NotNull]
        public string CalDAVPath { get; set; }


        public Server(string FQDN, string Protocol="https", string WebDAVPath="/remote.php/webdav/", string CalDAVPath="/remote.php/caldav/") {
            this.FQDN = FQDN;
            this.Protocol = Protocol;
            this.WebDAVPath = WebDAVPath;
            this.CalDAVPath = CalDAVPath;
        }

    }
}
