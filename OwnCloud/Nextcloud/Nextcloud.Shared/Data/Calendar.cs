using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Calendars")]
    public class Calendar : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int CalendarId { get; set; }

        public string DisplayName { get; set; }

        public string Path { get; set; }

        public string Color { get; set; }

        public string CTag { get; set; }

        [ForeignKey(typeof(Account))]
        public int AccountId { get; set; }

        [ManyToOne]
        public Account Account { get; set; }
    }
}
