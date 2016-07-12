using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Servers")]
    public class Server : IEntity
    {
        [PrimaryKey]
        public string FQDN { get; set; }

        [NotNull]
        public string Protocol { get; set; }

        [NotNull]
        public string WebDAVPath { get; set; }

        [NotNull]
        public string CalDAVPath { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Account> Accounts { get; set; }


        public Server() {
            this.WebDAVPath = "/remote.php/webdav/";
            this.CalDAVPath = "/remote.php/caldav/";
        }

    }
}
