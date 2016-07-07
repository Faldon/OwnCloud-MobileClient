using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nexcloud.Data
{
    [Table("Accounts")]
    class Account : IEntity
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
    }
}
