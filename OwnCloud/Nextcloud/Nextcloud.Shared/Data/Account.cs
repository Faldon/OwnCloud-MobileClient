using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

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
    }
}
