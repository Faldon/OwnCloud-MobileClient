
using System.Collections.Generic;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Nextcloud.Data
{
    [Table("Accounts")]
    public class Account : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int AccountId { get; set; }

        [NotNull]
        public bool IsEncrypted { get; set; }

        [NotNull]
        public string Username { get; set; }

        [NotNull]
        public string Password { get; set; }

        [NotNull]
        public bool IsAnonymous { get; set; }

        [NotNull]
        public bool IsCalendarEnabled { get; set; }

        [ForeignKey(typeof(Server))]
        public string ServerFQDN { get; set; }

        [ManyToOne]
        public Server Server { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<File> Files { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Calendar> Calendars { get; set; }

        public Uri GetWebDAVRoot()
        {
            return new Uri(Server.Protocol + "://" + Server.FQDN.TrimEnd('/') + Server.WebDAVPath, UriKind.Absolute);
        }

        public Uri GetCalDAVRoot() {
            return new Uri(Server.Protocol + "://" + Server.FQDN.TrimEnd('/') + Server.CalDAVPath + "calendars/" + Username + "/", UriKind.Absolute);
        }

        public async Task<NetworkCredential> GetCredential()
        {
            var _password = await Utility.DecryptString(Password);
            return new NetworkCredential(Username, _password);
        }
    }
}
